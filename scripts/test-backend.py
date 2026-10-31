#!/usr/bin/env python3
"""Run backend unit, architecture, PostgreSQL and HTTP tests against persistent test storage."""
import argparse
import os
from pathlib import Path
import subprocess

p = argparse.ArgumentParser(description=__doc__)
p.add_argument('--env-file', default='.env')
p.add_argument('--host', default='127.0.0.1')
p.add_argument('--port', type=int)
a = p.parse_args()
values = dict(line.split('=', 1) for line in Path(a.env_file).read_text().splitlines()
              if '=' in line and not line.lstrip().startswith('#'))
env = os.environ.copy()
env.update(PGHOST=a.host, PGPORT=str(a.port or values.get('TEST_DB_PORT', '55434')),
           PGDATABASE='comments_test', PGUSER='comments_app',
           PGPASSWORD=os.environ.get('TEST_DB_APP_PASSWORD') or values['TEST_DB_APP_PASSWORD'])
# The data and HTTP suites share persistent comments_test. Run projects sequentially:
# an HTTP commit must not race a data-suite baseline/count inside a rollback fixture.
for project in ['Domain', 'Application', 'Data', 'Api']:
    subprocess.run(['dotnet', 'test', f'tests/CommentsService.{project}.Tests', '-c', 'Release'],
                   env=env, check=True)
