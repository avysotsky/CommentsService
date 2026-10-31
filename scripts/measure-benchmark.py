#!/usr/bin/env python3
"""Read-only paging measurements against the retained paging-v1 benchmark corpus."""
import argparse
from datetime import datetime, timezone
import json
from pathlib import Path
import statistics
import os
import subprocess
import uuid
import urllib.error
import time
import urllib.parse
import urllib.request

p = argparse.ArgumentParser(description=__doc__)
p.add_argument('--base-url', default='http://127.0.0.1:8083')
p.add_argument('--start-api', action='store_true', help='Temporarily run the app against comments_benchmark on the loopback base URL.')
p.add_argument('--env-file', default='.env')
p.add_argument('--project', default='commentsservice')
p.add_argument('--compose-file', action='append')
p.add_argument('--samples', type=int, default=3)
p.add_argument('--expected-roots', type=int, default=800000)
p.add_argument('--output', default='artifacts/benchmark-paging-v1.json')
a = p.parse_args()
if not 1 <= a.samples <= 10 or a.expected_roots < 1: p.error('Use 1..10 samples and a positive expected root count.')
base = a.base_url.rstrip('/')
def get(path):
    with urllib.request.urlopen(base + path, timeout=60) as response: return json.load(response)
container = None
name = 'commentsservice-benchmark-' + uuid.uuid4().hex[:12]
try:
    if a.start_api:
        url = urllib.parse.urlparse(base)
        if url.scheme != 'http' or url.hostname not in ('127.0.0.1', 'localhost') or url.path not in ('', '/') or url.username or url.password:
            p.error('--start-api requires an HTTP loopback base URL.')
        values = dict(line.split('=', 1) for line in Path(a.env_file).read_text().splitlines()
                      if '=' in line and not line.lstrip().startswith('#'))
        env = os.environ.copy(); env['PGPASSWORD'] = os.environ.get('TEST_DB_APP_PASSWORD') or values['TEST_DB_APP_PASSWORD']
        compose = ['docker', 'compose']
        for file in a.compose_file or ['compose.yaml']: compose += ['-f', file]
        compose += ['--env-file', a.env_file, '-p', a.project, '--profile', 'testing']
        container = subprocess.check_output(compose + ['run', '-d', '--no-deps', '--name', name,
            '-p', '127.0.0.1:' + str(url.port or 80) + ':8080', '-e', 'PGHOST=test-database',
            '-e', 'PGDATABASE=comments_benchmark', '-e', 'Runtime__Search__Enabled=false', '-e', 'PGPASSWORD', 'app'], env=env).decode().strip()
        for _ in range(150):
            try:
                get('/health/database'); break
            except (urllib.error.URLError, ConnectionError): time.sleep(.2)
        else: raise RuntimeError('Benchmark API readiness timed out.')
    status = get('/api/status')
    total_pages = (a.expected_roots + 24) // 25
    report = {'dataset': 'paging-v1', 'version': 1, 'application': status,
              'measured_at_utc': datetime.now(timezone.utc).isoformat(), 'roots': a.expected_roots,
              'page_size': 25, 'samples': a.samples, 'results': []}
    get('/api/comments/page')  # warm up the runtime; omit startup from the recorded cases
    for sort in ['createdAt', 'userName', 'email']:
        for direction in ['asc', 'desc']:
            for page in sorted({1, max(1, total_pages // 2), total_pages}):
                query = urllib.parse.urlencode({'page': page, 'sort': sort, 'direction': direction})
                timings = []; stable_ids = None
                for _ in range(a.samples):
                    start = time.perf_counter(); data = get('/api/comments/page?' + query)
                    timings.append(round((time.perf_counter() - start) * 1000, 2))
                    assert data['totalCount'] == a.expected_roots and data['totalPages'] == total_pages
                    assert data['page'] == page and data['pageSize'] == 25
                    assert len(data['items']) == min(25, a.expected_roots - (page - 1) * 25)
                    ids = [item['id'] for item in data['items']]
                    assert len(set(ids)) == len(ids) and all(item['parentId'] is None and item['rootId'] == item['id'] for item in data['items'])
                    if stable_ids is not None: assert stable_ids == ids
                    stable_ids = ids
                row = {'sort': sort, 'direction': direction, 'page': page, 'milliseconds': timings,
                       'median_ms': round(statistics.median(timings), 2), 'max_ms': max(timings)}
                report['results'].append(row)
                print(f'{sort:9} {direction:4} page {page:5}: median {row["median_ms"]:8.2f} ms; max {row["max_ms"]:8.2f} ms', flush=True)
    out = Path(a.output); out.parent.mkdir(parents=True, exist_ok=True)
    out.write_text(json.dumps(report, indent=2) + '\n')
    print(f'PASS: {len(report["results"])} cases; repeated IDs stable; only roots; report {out}.', flush=True)
finally:
    if container:
        actual = subprocess.check_output(['docker', 'inspect', '--format', '{{.Id}}', name]).decode().strip()
        labels = json.loads(subprocess.check_output(['docker', 'inspect', '--format', '{{json .Config.Labels}}', name]))
        if actual != container or labels.get('com.docker.compose.project') != a.project or labels.get('com.docker.compose.service') != 'app':
            raise RuntimeError('Unexpected benchmark container ownership; refusing cleanup.')
        subprocess.run(['docker', 'rm', '-f', name], check=True, stdout=subprocess.DEVNULL)
