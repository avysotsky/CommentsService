"""Create local Docker credentials once, without printing or overwriting secrets."""
import argparse
import os
from pathlib import Path
import secrets
import sys

parser = argparse.ArgumentParser()
parser.add_argument('--output', type=Path, default=Path('.env'))
parser.add_argument('--port', type=int, default=8080)
parser.add_argument('--test-db-port', type=int, default=55434)
args = parser.parse_args()
if not (1 <= args.port <= 65535 and 1 <= args.test_db_port <= 65535):
    parser.error('Ports must be between 1 and 65535.')
required = ['DB_ADMIN_PASSWORD', 'DB_APP_PASSWORD', 'TEST_DB_ADMIN_PASSWORD', 'TEST_DB_APP_PASSWORD']
if args.output.exists():
    existing = dict(line.split('=', 1) for line in args.output.read_text().splitlines()
                    if '=' in line and not line.lstrip().startswith('#'))
    missing = [key for key in required if not existing.get(key)]
    if missing:
        print('Existing environment file is incomplete; it was not changed. Use a separate output file.', file=sys.stderr)
        sys.exit(1)
    print('Existing environment file preserved.')
    sys.exit(0)
args.output.parent.mkdir(parents=True, exist_ok=True)
values = {key: secrets.token_hex(24) for key in required}
values.update(APP_PORT=str(args.port), TEST_DB_PORT=str(args.test_db_port))
descriptor = os.open(args.output, os.O_WRONLY | os.O_CREAT | os.O_EXCL, 0o600)
with os.fdopen(descriptor, 'w', newline='\n') as stream:
    stream.write(''.join(f'{key}={value}\n' for key, value in values.items()))
print('Environment file created with random credentials. Keep it private.')
