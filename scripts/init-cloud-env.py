#!/usr/bin/env python3
"""Create a private empty Supabase runtime environment template; never overwrite credentials."""
import os
from pathlib import Path
p=Path('.env.cloud')
with p.open('x', encoding='utf-8') as f:
    f.write('SUPABASE_URL=\nSUPABASE_SECRET_KEY=\nSUPABASE_BUCKET=comments-attachments\n')
os.chmod(p,0o600)
print('Created private Cloud template. Fill it locally; never share or commit secrets.')
