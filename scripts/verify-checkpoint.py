#!/usr/bin/env python3
"""Restore an existing checkpoint into one owned temporary DB; optionally rehearse upgrade.
Never replaces the checkpoint or connects the app to the recovery database.
"""
import argparse
import hashlib
import json
from pathlib import Path
import re
import subprocess
import uuid

p = argparse.ArgumentParser(description=__doc__)
p.add_argument('dump', type=Path)
p.add_argument('--manifest', type=Path)
p.add_argument('--env-file', default='.env')
p.add_argument('--project', default='commentsservice')
p.add_argument('--compose-file', action='append')
p.add_argument('--service', choices=['database','test-database'], default='test-database')
p.add_argument('--upgrade', action='store_true')
p.add_argument('--output', type=Path)
a = p.parse_args()
root = Path(__file__).resolve().parent.parent
manifest = json.loads((a.manifest or a.dump.with_suffix('.json')).read_text(encoding='utf-8-sig'))
def digest():
    value=hashlib.sha256()
    with a.dump.open('rb') as source:
        for chunk in iter(lambda: source.read(1024*1024), b''): value.update(chunk)
    return value.hexdigest()
if digest() != manifest['sha256']:
    raise RuntimeError('Checkpoint SHA-256 differs; no recovery database created.')
compose=['docker','compose']
for file in a.compose_file or ['compose.yaml']: compose += ['-f',file]
compose += ['--env-file',a.env_file,'-p',a.project,'--profile','testing']
def sql(database, text):
    return subprocess.check_output(compose+['exec','-T',a.service,'psql','-X','-U','postgres','-d',database,
        '-Atq','-v','ON_ERROR_STOP=1'],input=text.encode()).decode().strip()
def inventory(database):
    tables=json.loads(sql(database,"""SELECT coalesce(json_agg(x ORDER BY x.s,x.t),'[]') FROM
     (SELECT schemaname s,tablename t FROM pg_tables WHERE schemaname NOT IN ('pg_catalog','information_schema')) x;"""))
    result={}
    for table in tables:
        qualified='.'.join('"'+table[k].replace('"','""')+'"' for k in ('s','t'))
        result[qualified]=sql(database,"SELECT count(*)::text || '|' || coalesce(md5(string_agg(h,'' ORDER BY h)),'empty') "
          +'FROM (SELECT md5(to_jsonb(t)::text) h FROM '+qualified+' t) rows;')
    return result
name='cs_acceptance_restore_'+uuid.uuid4().hex
created=False
try:
    sql('postgres','CREATE DATABASE '+name+' OWNER comments_app;');created=True
    with a.dump.open('rb') as source:
        subprocess.run(compose+['exec','-T',a.service,'pg_restore','-U','postgres','--role=comments_app',
          '--exit-on-error','--no-owner','--no-privileges','-d',name],stdin=source,check=True)
    before=inventory(name)
    if before != manifest['tables']: raise RuntimeError('Restored tables differ from checkpoint manifest.')
    history_key='"comments"."__EFMigrationsHistory"'
    old_history=json.loads(sql(name,'SELECT json_agg(h ORDER BY "MigrationId") FROM comments."__EFMigrationsHistory" h;')) if history_key in before else []
    after=before
    if a.upgrade:
        sql(name,'SET ROLE comments_app;\n'+(root/'docs/database/postgresql.sql').read_text(encoding='utf-8-sig'))
        after=inventory(name)
        if any(after.get(table)!=value for table,value in before.items() if table!=history_key):
            raise RuntimeError('Upgrade changed existing data.')
        history=json.loads(sql(name,'SELECT json_agg(h ORDER BY "MigrationId") FROM comments."__EFMigrationsHistory" h;'))
        if history[:len(old_history)]!=old_history: raise RuntimeError('Previous migration history changed.')
        expected=sorted(file.stem for file in (root/'src/CommentsService.Infrastructure/Persistence/Migrations').glob('*.cs')
            if file.name[0].isdigit() and not file.name.endswith('.Designer.cs'))
        if [h['MigrationId'] for h in history]!=expected: raise RuntimeError('Upgrade history is incomplete.')
        # Applying the shipped idempotent export again must preserve every row/hash.
        sql(name,'SET ROLE comments_app;\n'+(root/'docs/database/postgresql.sql').read_text(encoding='utf-8-sig'))
        if after!=inventory(name): raise RuntimeError('Repeated export changed data.')
    if digest()!=manifest['sha256']: raise RuntimeError('Original checkpoint changed.')
    incorrect_owners=sql(name,"""SELECT count(*) FROM pg_class t JOIN pg_namespace n ON n.oid=t.relnamespace
     WHERE n.nspname='comments' AND t.relkind='r' AND pg_get_userbyid(t.relowner)<>'comments_app';""")
    if incorrect_owners!='0': raise RuntimeError('Restored/upgraded tables are not owned by the application role.')
    report={'passed':True,'source_database':manifest['database'],'restore_verified':True,
       'upgrade_verified':a.upgrade,'application_role_owns_tables':True,'before_tables':before,'after_tables':after,'original_dump_unchanged':True}
    if a.output:a.output.write_text(json.dumps(report,indent=2)+'\n')
    print('PASS: checkpoint restored; row hashes match; original unchanged'+('; upgrade/idempotency preserved data.' if a.upgrade else '.'))
finally:
    if created:
        if not re.fullmatch(r'cs_acceptance_restore_[0-9a-f]{32}',name):raise RuntimeError('Invalid owned recovery DB name.')
        owner=sql('postgres',"SELECT pg_get_userbyid(datdba) FROM pg_database WHERE datname='"+name+"';")
        if owner!='comments_app':raise RuntimeError('Recovery DB owner changed; refusing cleanup.')
        sql('postgres','DROP DATABASE '+name+';')
