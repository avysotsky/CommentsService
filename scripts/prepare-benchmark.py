#!/usr/bin/env python3
"""Prepare an opt-in, persistent, deterministic paging corpus in a separate benchmark database."""
import argparse
import json
import os
from pathlib import Path
import subprocess

p = argparse.ArgumentParser(description=__doc__)
p.add_argument('--env-file', default='.env')
p.add_argument('--project', default='commentsservice')
p.add_argument('--compose-file', action='append')
p.add_argument('--rows', type=int, default=1000000)
a = p.parse_args()
if not 100 <= a.rows <= 1000000: p.error('--rows must be 100..1000000; use the same count on later runs.')
compose = ['docker', 'compose']
for file in a.compose_file or ['compose.yaml']: compose += ['-f', file]
compose += ['--env-file', a.env_file, '-p', a.project, '--profile', 'testing']
container = subprocess.check_output(compose + ['ps', '-q', 'test-database']).decode().strip()
if not container: raise RuntimeError('Start the persistent test-database service first.')
labels = json.loads(subprocess.check_output(['docker', 'inspect', '--format', '{{json .Config.Labels}}', container]))
if labels.get('com.docker.compose.project') != a.project or labels.get('com.docker.compose.service') != 'test-database':
    raise RuntimeError('Unexpected database container ownership.')

def sql(statement, database='postgres', app=False):
    command = ['psql', '-X', '-U', 'postgres', '-d', database, '-Atq', '-v', 'ON_ERROR_STOP=1']
    if app:
        # The existing private password stays inside its database container.
        command = ['bash', '-c', 'PGPASSWORD="$APP_DB_PASSWORD" exec psql -h 127.0.0.1 -X -U comments_app -d comments_benchmark -Atq -v ON_ERROR_STOP=1']
    return subprocess.run(compose + ['exec', '-T', 'test-database'] + command,
                          input=statement.encode(), stdout=subprocess.PIPE, check=True).stdout.decode().strip()

owner = sql("SELECT pg_get_userbyid(datdba) FROM pg_database WHERE datname='comments_benchmark';")
if not owner:
    sql('CREATE DATABASE comments_benchmark OWNER comments_app;')
    print('Created the dedicated persistent benchmark database.', flush=True)
elif owner != 'comments_app': raise RuntimeError('Existing benchmark database has an unexpected owner.')
common = ['--env-file', a.env_file, '--project', a.project]
for file in a.compose_file or ['compose.yaml']: common += ['--compose-file', file]
subprocess.run(['python3', 'scripts/db-command.py', 'migrate', '--benchmark'] + common, check=True)
roots = a.rows * 4 // 5
authors = min(10000, roots)
exists = sql("SELECT to_regclass('benchmark.\"Dataset\"') IS NOT NULL;", app=True) == 't'
if exists:
    record = json.loads(sql('SELECT row_to_json(d) FROM benchmark."Dataset" d WHERE "Name"=\'paging-v1\';', app=True) or 'null')
    if not record or record['Version'] != 1 or record['ExpectedRows'] != a.rows or not record['Completed']:
        raise RuntimeError('Existing dataset metadata differs; never replace or regenerate it implicitly.')
    counts = sql('SELECT count(*)::text || \'|\' || count(*) FILTER (WHERE "ParentId" IS NULL)::text FROM comments."Comments";', app=True)
    if counts != f'{a.rows}|{roots}': raise RuntimeError('Persistent corpus count changed; inspect before reuse.')
    print(f'REUSED paging-v1: {a.rows} comments, {roots} roots; no corpus writes.', flush=True)
    raise SystemExit(0)
if sql('SELECT count(*) FROM comments."Comments";', app=True) != '0' or sql('SELECT count(*) FROM comments."Authors";', app=True) != '0':
    raise RuntimeError('Unmarked data exists in benchmark storage; refusing to mix or overwrite it.')
# One transaction and a database-local advisory lock make seeding atomic and concurrent-safe.
statement = f"""
BEGIN;
SELECT pg_advisory_xact_lock(813080001);
DO $check$ BEGIN
 IF to_regclass('benchmark."Dataset"') IS NOT NULL THEN
  RAISE EXCEPTION 'Another process prepared the dataset; rerun to reuse it';
 END IF;
END $check$;
CREATE SCHEMA benchmark AUTHORIZATION comments_app;
CREATE TABLE benchmark."Dataset" (
 "Name" text PRIMARY KEY, "Version" integer NOT NULL, "ExpectedRows" integer NOT NULL,
 "Completed" boolean NOT NULL, "CreatedAt" timestamptz NOT NULL DEFAULT now()
);
INSERT INTO comments."Authors" ("Id","UserName","Email")
 SELECT md5('cs-paging-v1.author.'||i)::uuid, 'BenchUser'||lpad(i::text,5,'0'), 'bench'||lpad(i::text,5,'0')||'@example.test'
 FROM generate_series(1,{authors}) i;
DO $seed$ DECLARE first_row integer; BEGIN
 FOR first_row IN SELECT generate_series(1,{a.rows},25000) LOOP
  INSERT INTO comments."Comments" ("Id","AuthorId","Text","CreatedAt","ClientIp","UserAgent","ParentId","RootId")
  SELECT md5('cs-paging-v1.comment.'||i)::uuid,
   md5('cs-paging-v1.author.'||((i-1)%{authors}+1))::uuid,
   'Benchmark paging-v1 item '||i, timestamptz '2025-01-01 00:00:00+00' + i * interval '1 second',
   '127.0.0.1'::inet, 'CommentsService.Benchmark/v1',
   CASE WHEN i<={roots} THEN NULL
    WHEN (i-{roots}-1)%4=0 THEN md5('cs-paging-v1.comment.'||((i-{roots}-1)/4+1))::uuid
    ELSE md5('cs-paging-v1.comment.'||(i-1))::uuid END,
   md5('cs-paging-v1.comment.'||CASE WHEN i<={roots} THEN i ELSE (i-{roots}-1)/4+1 END)::uuid
  FROM generate_series(first_row,least(first_row+24999,{a.rows})) i ORDER BY i;
  RAISE NOTICE 'paging-v1 rows: % / {a.rows}',least(first_row+24999,{a.rows});
 END LOOP;
END $seed$;
INSERT INTO benchmark."Dataset" ("Name","Version","ExpectedRows","Completed") VALUES ('paging-v1',1,{a.rows},true);
COMMIT;
ANALYZE comments."Authors";
ANALYZE comments."Comments";
"""
sql(statement, app=True)
counts = sql('SELECT count(*)::text || \'|\' || count(*) FILTER (WHERE "ParentId" IS NULL)::text FROM comments."Comments";', app=True)
if counts != f'{a.rows}|{roots}': raise RuntimeError('Seed verification failed.')
print(f'PREPARED paging-v1: {a.rows} comments, {roots} roots, {a.rows-roots} replies; retained for future runs.', flush=True)
