"""Generate optional private RabbitMQ credentials without altering existing application secrets."""
import argparse
import os
from pathlib import Path
import secrets

parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument('--output', type=Path, default=Path('.env.broker'))
args = parser.parse_args()
if args.output.exists():
    existing = dict(line.split('=',1) for line in args.output.read_text().splitlines() if '=' in line and not line.lstrip().startswith('#'))
    if not existing.get('BROKER_PASSWORD') or existing.get('BROKER_PASSWORD') != existing.get('RABBITMQ_DEFAULT_PASS') or existing.get('Runtime__Broker__Username') != existing.get('RABBITMQ_DEFAULT_USER'):
        parser.exit(1, 'Existing broker environment is incomplete; it was not modified.\n')
    parser.exit(0, 'Existing broker environment preserved; no credentials changed.\n')
args.output.parent.mkdir(parents=True, exist_ok=True)
password = secrets.token_hex(24)
values = {'Runtime__Broker__Enabled': 'true', 'Runtime__Broker__Host': 'broker', 'Runtime__Broker__Username': 'comments_app',
          'BROKER_PASSWORD': password, 'RABBITMQ_DEFAULT_USER': 'comments_app', 'RABBITMQ_DEFAULT_PASS': password}
fd = os.open(args.output, os.O_WRONLY | os.O_CREAT | os.O_EXCL, 0o600)
with os.fdopen(fd, 'w', newline='\n') as file:
    file.write(''.join(f'{key}={value}\n' for key,value in values.items()))
print('Private broker environment created; keep it out of source control.')
