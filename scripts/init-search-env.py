"""Create separate optional app and Elasticsearch bootstrap credentials without overwriting files."""
import argparse,os,secrets
from pathlib import Path
p=argparse.ArgumentParser(description=__doc__)
p.add_argument('--app-output',type=Path,default=Path('.env.search'))
p.add_argument('--server-output',type=Path,default=Path('.env.elasticsearch'))
a=p.parse_args()
if a.app_output.resolve()==a.server_output.resolve(): p.error('Use two different private files.')
for file,values in [(a.app_output,{'Runtime__Search__Enabled':'true','Runtime__Search__Endpoint':'http://elasticsearch:9200','Runtime__Search__Username':'comments_search','SEARCH_PASSWORD':secrets.token_hex(24)}),
                    (a.server_output,{'ELASTIC_PASSWORD':secrets.token_hex(24)})]:
    if file.exists():
        current=dict(line.split('=',1) for line in file.read_text().splitlines() if '=' in line and not line.lstrip().startswith('#'))
        required='SEARCH_PASSWORD' if file==a.app_output else 'ELASTIC_PASSWORD'
        if not current.get(required): p.exit(1,'Existing search environment is incomplete; it was not changed.\n')
        continue
    file.parent.mkdir(parents=True,exist_ok=True)
    fd=os.open(file,os.O_WRONLY|os.O_CREAT|os.O_EXCL,0o600)
    with os.fdopen(fd,'w',newline='\n') as f:f.write(''.join(f'{k}={v}\n' for k,v in values.items()))
print('Private search environments ready; existing files preserved.')
