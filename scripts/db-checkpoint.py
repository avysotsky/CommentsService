#!/usr/bin/env python3
"""Create and restore-verify one previous recovery point per Compose database."""
import argparse
from datetime import datetime, timezone
import hashlib
import json
import os
from pathlib import Path
import subprocess
import uuid

p = argparse.ArgumentParser(description=__doc__)
p.add_argument('--env-file', default='.env')
p.add_argument('--project', default='commentsservice')
p.add_argument('--compose-file', action='append')
p.add_argument('--output', default='artifacts/checkpoints')
p.add_argument('--include-benchmark', action='store_true', help='Also checkpoint the persistent comments_benchmark database.')
a = p.parse_args()
compose = ['docker', 'compose']
for file in a.compose_file or ['compose.yaml']:
    compose += ['-f', file]
compose += ['--env-file', a.env_file, '-p', a.project, '--profile', 'testing']
os.umask(0o077)
out = Path(a.output)
out.mkdir(parents=True, exist_ok=True)

def run(service, command, *, data=None, output=None):
    # Credentials stay inside the database container. Never print environment/config.
    return subprocess.run(compose + ['exec', '-T', service, 'bash', '-c', command],
                          input=data, stdout=output or subprocess.PIPE,
                          stderr=subprocess.PIPE, check=True).stdout

def sql(service, database, statement):
    return run(service, 'exec psql -X -U postgres -d ' + database + ' -Atq -v ON_ERROR_STOP=1',
               data=statement.encode()).decode().strip()

def inventory(service, database):
    tables = json.loads(sql(service, database, """
SELECT coalesce(json_agg(x ORDER BY x.s, x.t), '[]') FROM (
 SELECT schemaname AS s, tablename AS t FROM pg_tables
 WHERE schemaname NOT IN ('pg_catalog', 'information_schema')
) x;
"""))
    result = {}
    for table in tables:
        qualified = '.'.join('"' + table[k].replace('"', '""') + '"' for k in ('s', 't'))
        result[qualified] = sql(service, database,
            'SELECT count(*)::text || \'|\' || coalesce(md5(string_agg(h, \'\' ORDER BY h)), \'empty\') '
            'FROM (SELECT md5(to_jsonb(t)::text) h FROM ' + qualified + ' t) rows;')
    return result

targets = [('database', 'comments', 'database'), ('test-database', 'comments_test', 'test-database')]
if a.include_benchmark: targets.append(('test-database', 'comments_benchmark', 'benchmark'))
for service, database, checkpoint_name in targets:
    temporary = 'cs_checkpoint_' + uuid.uuid4().hex
    candidate = out / (checkpoint_name + '.candidate.dump')
    created = False
    try:
        before = inventory(service, database)
        with candidate.open('wb') as f:
            run(service, 'exec pg_dump -U postgres -d ' + database + ' -Fc', output=f)
        sql(service, 'postgres', 'CREATE DATABASE ' + temporary + ';')
        created = True
        with candidate.open('rb') as f:
            subprocess.run(compose + ['exec', '-T', service, 'pg_restore', '-U', 'postgres',
                '--exit-on-error', '--no-owner', '--no-privileges', '-d', temporary],
                stdin=f, stdout=subprocess.PIPE, stderr=subprocess.PIPE, check=True)
        restored = inventory(service, temporary)
        after = inventory(service, database)
        if before != restored or before != after:
            raise RuntimeError('Checkpoint differs from source: ' + service)
        manifest = {'database': database, 'created_at_utc': datetime.now(timezone.utc).isoformat(), 'tables': restored,
                    'sha256': hashlib.sha256(candidate.read_bytes()).hexdigest(),
                    'restore_verified': True}
        manifest_candidate = out / (checkpoint_name + '.candidate.json')
        manifest_candidate.write_text(json.dumps(manifest, indent=2) + '\n')
        os.replace(candidate, out / (checkpoint_name + '.dump'))
        os.replace(manifest_candidate, out / (checkpoint_name + '.json'))
        print('PASS: checkpoint restored and row fingerprints verified for ' + database, flush=True)
    finally:
        if created:
            # Only the uniquely named database created above belongs to this run.
            sql(service, 'postgres', 'DROP DATABASE ' + temporary + ';')
