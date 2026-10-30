#!/usr/bin/env python3
"""Read-only comparison of PostgreSQL metadata with the shipped Workbench projection."""
import argparse
import json
from pathlib import Path
import re
import subprocess

p = argparse.ArgumentParser(description=__doc__)
p.add_argument('--env-file', default='.env')
p.add_argument('--project', default='commentsservice')
p.add_argument('--compose-file', action='append')
p.add_argument('--service', choices=['database', 'test-database'], default='database')
p.add_argument('--database', default='comments')
p.add_argument('--output')
a = p.parse_args()
if not re.fullmatch(r'[a-z_][a-z0-9_]{0,62}', a.database):
    p.error('Use a simple PostgreSQL database name.')
root = Path(__file__).resolve().parent.parent
compose = ['docker', 'compose']
for file in a.compose_file or ['compose.yaml']:
    compose += ['-f', file]
compose += ['--env-file', a.env_file, '-p', a.project, '--profile', 'testing']
def query(sql):
    return json.loads(subprocess.check_output(compose + ['exec', '-T', a.service,
        'psql', '-X', '-U', 'postgres', '-d', a.database, '-Atq', '-v', 'ON_ERROR_STOP=1'], input=sql.encode()).decode())

columns = query("""SELECT json_agg(x) FROM (SELECT table_name, column_name, udt_name,
 character_maximum_length, is_nullable FROM information_schema.columns
 WHERE table_schema='comments' ORDER BY table_name,ordinal_position) x;""")
indexes = query("""SELECT json_agg(x) FROM (SELECT tablename,indexdef FROM pg_indexes
 WHERE schemaname='comments' ORDER BY tablename,indexname) x;""")
fks = query("""SELECT json_agg(x) FROM (SELECT t.relname AS table_name,r.relname AS target,
 c.confdeltype AS deletion,
 ARRAY(SELECT a.attname FROM unnest(c.conkey) WITH ORDINALITY k(n,o)
 JOIN pg_attribute a ON a.attrelid=c.conrelid AND a.attnum=k.n ORDER BY k.o) AS columns,
 ARRAY(SELECT a.attname FROM unnest(c.confkey) WITH ORDINALITY k(n,o)
 JOIN pg_attribute a ON a.attrelid=c.confrelid AND a.attnum=k.n ORDER BY k.o) AS referenced
 FROM pg_constraint c JOIN pg_class t ON t.oid=c.conrelid JOIN pg_class r ON r.oid=c.confrelid
 JOIN pg_namespace s ON s.oid=t.relnamespace WHERE s.nspname='comments' AND c.contype='f') x;""")
primary = query("""SELECT json_agg(x) FROM (SELECT t.relname AS table_name,
 ARRAY(SELECT a.attname FROM unnest(c.conkey) WITH ORDINALITY k(n,o)
 JOIN pg_attribute a ON a.attrelid=c.conrelid AND a.attnum=k.n ORDER BY k.o) AS columns
 FROM pg_constraint c JOIN pg_class t ON t.oid=c.conrelid JOIN pg_namespace n ON n.oid=t.relnamespace
 WHERE n.nspname='comments' AND c.contype='p') x;""")
checks = query("""SELECT json_agg(c.conname ORDER BY c.conname) FROM pg_constraint c
 JOIN pg_namespace n ON n.oid=c.connamespace WHERE n.nspname='comments' AND c.contype='c';""")
history = query('SELECT json_agg("MigrationId" ORDER BY "MigrationId") FROM comments."__EFMigrationsHistory";')
workbench = (root / 'docs/database/workbench.sql').read_text(encoding='utf-8-sig')
blocks = dict(re.findall(r'CREATE TABLE `([^`]+)`\s*\((.*?)\);', workbench, re.S))
assert set(blocks) == {c['table_name'] for c in columns}, 'Table set differs'
def normalized_columns(text):
    return tuple((m.group(1), bool(m.group(2))) for m in re.finditer(r'`([^`]+)`\s*(DESC)?', text, re.I))
projected_columns = {}
projected_indexes = set()
projected_primary = set()
projected_fks = set()
for table, body in blocks.items():
    for line in body.splitlines():
        match = re.match(r'\s*`([^`]+)`\s+(\w+(?:\(\d+(?:,\d+)?\))?)\s+(NOT NULL|NULL)', line)
        if match:
            projected_columns[(table, match[1])] = (match[2].upper(), match[3] == 'NULL')
    for match in re.finditer(r'(PRIMARY KEY|UNIQUE KEY `[^`]+`|INDEX `[^`]+`)\s*\(([^)]+)\)', body):
        projected_indexes.add((table, match[1].startswith(('PRIMARY','UNIQUE')), normalized_columns(match[2])))
        if match[1] == 'PRIMARY KEY': projected_primary.add((table,tuple(n for n,_ in normalized_columns(match[2]))))
    for match in re.finditer(r'FOREIGN KEY\s*\(([^)]+)\)\s*REFERENCES `([^`]+)`\s*\(([^)]+)\) ON DELETE (RESTRICT|CASCADE)', body):
        projected_fks.add((table, tuple(n for n,_ in normalized_columns(match[1])), match[2],
            tuple(n for n,_ in normalized_columns(match[3])), match[4]))
actual_columns = {}
for c in columns:
    kind, length = c['udt_name'], c['character_maximum_length']
    converted = {'uuid':'CHAR(36)', 'inet':'VARCHAR(45)', 'timestamptz':'DATETIME(6)', 'int4':'INT', 'bytea':'MEDIUMBLOB'}.get(kind)
    if kind == 'varchar': converted = 'MEDIUMTEXT' if length == 65536 else f'VARCHAR({length})'
    assert converted is not None, f'Unmapped type {kind}'
    actual_columns[(c['table_name'], c['column_name'])] = (converted, c['is_nullable'] == 'YES')
assert actual_columns == projected_columns, 'Column types/nullability differ'
actual_indexes = set()
for index in indexes:
    definition = index['indexdef']
    tail = definition[definition.index('USING btree (') + len('USING btree ('):-1].replace('"', '`')
    actual_indexes.add((index['tablename'], 'CREATE UNIQUE INDEX' in definition, normalized_columns(tail)))
assert len(actual_indexes) == len(indexes), 'Unexpected duplicate equivalent PostgreSQL indexes'
assert actual_indexes == projected_indexes, 'Index uniqueness/columns/directions differ'
assert {(row['table_name'],tuple(row['columns'])) for row in primary} == projected_primary, 'Primary keys differ'
actual_fks = {(f['table_name'], tuple(f['columns']), f['target'], tuple(f['referenced']),
    {'r':'RESTRICT','c':'CASCADE'}.get(f['deletion'],f['deletion'])) for f in fks}
assert actual_fks == projected_fks, 'Foreign-key columns/targets/delete rules differ'
expected_checks = set(re.findall(r'CONSTRAINT "(CK_[^"]+)"', (root / 'docs/database/postgresql.sql').read_text(encoding='utf-8-sig')))
assert set(checks) == expected_checks, 'PostgreSQL checks differ from the shipped export'
expected_history = sorted(file.stem for file in (root / 'src/CommentsService.Infrastructure/Persistence/Migrations').glob('*.cs')
    if file.name[0].isdigit() and not file.name.endswith('.Designer.cs'))
assert history == expected_history, 'Migration history differs'
report = {'passed':True,'tables':len(blocks),'columns':len(actual_columns),'indexes':len(actual_indexes),
 'foreign_keys':len(actual_fks),'primary_keys':len(primary),'checks':len(checks),'migrations':history,
 'scope':'Metadata parity; does not certify GUI import in MySQL Workbench.'}
if a.output: Path(a.output).write_text(json.dumps(report,indent=2)+'\n')
print('PASS: PostgreSQL and Workbench projection agree on tables/columns/types/nullability/indexes/FKs; checks/export and migration history agree.')
