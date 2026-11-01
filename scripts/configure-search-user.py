"""Provision a namespace-limited Elasticsearch user in an explicitly owned Compose service."""
import argparse,json,subprocess,time
from pathlib import Path
p=argparse.ArgumentParser(description=__doc__)
p.add_argument('--app-env',type=Path,default=Path('.env.search'))
p.add_argument('--server-env',type=Path,default=Path('.env.elasticsearch'))
p.add_argument('--project',default='commentsservice')
p.add_argument('--compose-file',action='append')
p.add_argument('--env-file',default='.env')
a=p.parse_args()
def values(path):return dict(line.split('=',1) for line in path.read_text().splitlines() if '=' in line and not line.lstrip().startswith('#'))
app=values(a.app_env);server=values(a.server_env)
username=app.get('Runtime__Search__Username','comments_search')
if username!='comments_search':p.error('This provisioning script only manages comments_search.')
compose=['docker','compose']
for file in a.compose_file or ['compose.yaml']:compose+=['-f',file]
compose+=['--env-file',a.env_file,'-p',a.project,'--profile','search']
container=subprocess.check_output(compose+['ps','-q','elasticsearch']).decode().strip()
if not container:p.exit(1,'Owned search service is not running.\n')
i=json.loads(subprocess.check_output(['docker','inspect',container]))[0]
if i['Config']['Labels'].get('com.docker.compose.project')!=a.project or i['Config']['Labels'].get('com.docker.compose.service')!='elasticsearch':p.exit(1,'Search ownership check failed.\n')
for path,body in [('/_security/role/comments_search',{'cluster':[],'indices':[{'names':['comments-*'],'privileges':['read','write','create_index','view_index_metadata','monitor']}]}),
                  ('/_security/user/comments_search',{'password':app['SEARCH_PASSWORD'],'roles':['comments_search']})]:
    # curl reads credentials and JSON from stdin; neither secret appears in process arguments or output.
    config='url = '+json.dumps('http://localhost:9200'+path)+'\nuser = '+json.dumps('elastic:'+server['ELASTIC_PASSWORD'])+'\nrequest = "PUT"\nheader = "Content-Type: application/json"\ndata = '+json.dumps(json.dumps(body))+'\n'
    # An unauthenticated401 health check can precede security-index readiness on a new volume.
    # Retry only temporary failures, with a total120s deadline per idempotent operation.
    deadline=time.monotonic()+120
    config+='connect-timeout = 2\nmax-time = 5\nwrite-out = "\\n%{http_code}"\n'
    while True:
        r=subprocess.run(['docker','exec','-i',container,'curl','--silent','--show-error','--fail','--config','-'],input=config.encode(),stdout=subprocess.PIPE,stderr=subprocess.PIPE)
        try: status=int(r.stdout.rsplit(b'\n',1)[-1])
        except ValueError: status=0
        if r.returncode==0 and status in (200,201): break
        if (status not in (0,408,429,500,502,503,504) or time.monotonic()>=deadline):
            p.exit(1,'Search user provisioning failed or readiness deadline expired; private files preserved.\n')
        time.sleep(min(2,max(0,deadline-time.monotonic())))
print('Scoped comments_search role/user ready; admin credentials are not supplied to the app.')
