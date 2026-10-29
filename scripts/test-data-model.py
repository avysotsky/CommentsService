#!/usr/bin/env python3
"""Run transaction-isolated EF tests against the persistent test database."""
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
subprocess.run(['dotnet', 'test', 'tests/CommentsService.Data.Tests', '-c', 'Release'],
               env=env, check=True)
