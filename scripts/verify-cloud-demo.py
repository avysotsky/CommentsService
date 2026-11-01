#!/usr/bin/env python3
"""Read-only verification of two explicitly owner-selected Cloud demo comments.
No uploads, SQL writes, bucket scans, retries or secrets in command arguments/output.
"""
import argparse
import hashlib
import json
from pathlib import Path
import re
import sys
import urllib.error
import urllib.parse
import urllib.request
import uuid

p = argparse.ArgumentParser(description=__doc__)
p.add_argument('--base-url', default='http://127.0.0.1:8080')
p.add_argument('--env-file', type=Path, default=Path('.env.cloud'))
p.add_argument('--database', default='comments', help='Actual application database name, for object partitioning')
p.add_argument('--expected-author', required=True, help='Your own demo username, not an authentication claim')
p.add_argument('--comment-id', action='append', required=True, help='Own demo comment UUID; provide twice')
a = p.parse_args()
class NoRedirect(urllib.request.HTTPRedirectHandler):
    def redirect_request(self, req, fp, code, msg, headers, newurl):
        return None
opener = urllib.request.build_opener(NoRedirect)
remaining = 2 * 1024 * 1024

def read(url, maximum, key=None):
    global remaining
    req = urllib.request.Request(url, headers={'apikey': key} if key else {})
    try:
        with opener.open(req, timeout=5) as r:
            limit = min(maximum, remaining)
            data = r.read(limit + 1)
            if len(data) > limit:
                raise ValueError('Demo response exceeded its budget')
            remaining -= len(data)
            return r.status, data, dict(r.headers.items())
    except urllib.error.HTTPError as e:
        # Do not print provider URLs, bodies, credentials or exception messages.
        status = e.code
        e.close()
        return status, b'', {}

def main():
    base = urllib.parse.urlsplit(a.base_url)
    if base.scheme not in ('http', 'https') or base.hostname not in ('127.0.0.1', 'localhost') or base.username or base.password or base.query or base.fragment or base.path not in ('', '/'):
        raise ValueError('Use the local application origin')
    if len(a.comment_id) != 2 or len(set(a.comment_id)) != 2:
        raise ValueError('Select exactly two different own demo comments')
    ids = [str(uuid.UUID(value)) for value in a.comment_id]
    values = {}
    for line in a.env_file.read_text(encoding='utf-8-sig').splitlines():
        if line.strip() and not line.lstrip().startswith('#') and '=' in line:
            k, v = line.split('=', 1)
            values[k.strip()] = v.strip().strip('"').strip("'")
    cloud = values.get('SUPABASE_URL', '').rstrip('/')
    key = values.get('SUPABASE_SECRET_KEY', '')
    bucket = values.get('SUPABASE_BUCKET', '')
    if not re.fullmatch(r'https://[a-z0-9-]+\.supabase\.co', cloud) or not re.fullmatch(r'sb_secret_[A-Za-z0-9_-]+', key) or not re.fullmatch(r'[a-z0-9][a-z0-9-]{0,62}', bucket):
        raise ValueError('Invalid private Cloud settings')
    code, data, _ = read(cloud + '/storage/v1/bucket/' + bucket, 16384, key)
    if code != 200:
        raise ValueError('Private bucket unavailable')
    meta = json.loads(data)
    if meta.get('public') is not False or sorted(meta.get('allowed_mime_types') or []) != ['image/png', 'text/plain']:
        raise ValueError('Bucket privacy or MIME restrictions differ')
    partition = hashlib.sha256(a.database.encode()).hexdigest()[:12]
    results = []
    for comment_id in ids:
        code, data, _ = read(a.base_url.rstrip('/') + '/api/comments/' + comment_id, 131072)
        if code != 200:
            raise ValueError('Own demo comment unavailable')
        comment = json.loads(data)
        if comment.get('id') != comment_id or comment.get('userName') != a.expected_author:
            raise ValueError('Demo comment ownership marker differs')
        image, text = comment.get('image'), comment.get('textFile')
        if bool(image) == bool(text):
            raise ValueError('Each demo comment must have exactly one attachment')
        item = image or text
        file_id = str(uuid.UUID(item['id']))
        kind, extension, route, maximum = ('images', 'png', 'images', 524288) if image else ('text', 'txt', 'text-files', 102400)
        code, original, _ = read(a.base_url.rstrip('/') + '/api/' + route + '/' + file_id, maximum)
        if code != 200 or not original:
            raise ValueError('Backend attachment unavailable')
        digest = hashlib.sha256(original).hexdigest()
        object_key = f'v1/{partition}/{kind}/{file_id.replace("-", "")}/{digest}.{extension}'
        code, mirrored, _ = read(cloud + '/storage/v1/object/authenticated/' + bucket + '/' + object_key, maximum, key)
        if code != 200 or mirrored != original:
            raise ValueError('Real Cloud object missing or bytes differ')
        code, _, _ = read(cloud + '/storage/v1/object/public/' + bucket + '/' + object_key, 8192)
        if code not in (400, 401, 403, 404):
            raise ValueError('Public access was not denied')
        results.append({'commentId': comment_id, 'attachmentId': file_id, 'objectKey': object_key, 'bytes': len(original), 'sha256': digest})
    if len({x['objectKey'].split('/')[2] for x in results}) != 2:
        raise ValueError('Select one PNG and one TXT comment')
    print(json.dumps({'passed': True, 'realCloud': True, 'readOnly': True, 'publicAccessDenied': True, 'files': results}, indent=2))

try:
    main()
except Exception:
    print('Cloud demo verification failed. Check own IDs, local app, private settings, consent/replay and backend logs. Secrets and provider details were not printed.', file=sys.stderr)
    sys.exit(1)
