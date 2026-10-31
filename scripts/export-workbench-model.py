import json
from pathlib import Path
import xml.etree.ElementTree as ElementTree

import grt

root = Path(__file__).resolve().parents[1]
version = ElementTree.parse(root / 'src/CommentsService.Api/CommentsService.Api.csproj').findtext('.//Version')
grt.modules.Workbench.newDocument()
document = grt.root.wb.doc
model = document.physicalModels[0]
errors = grt.modules.MysqlSqlFacade.parseSqlScriptString(model.catalog,
    (root / 'docs/database/workbench.sql').read_text(encoding='utf-8-sig'))
if errors:
    raise RuntimeError('Workbench reported SQL parsing errors.')
schema = next(schema for schema in model.catalog.schemata if schema.name == 'comments')
counts = {'tables': len(schema.tables), 'columns': sum(len(table.columns) for table in schema.tables),
    'indexes': sum(len(table.indices) for table in schema.tables),
    'foreign_keys': sum(len(table.foreignKeys) for table in schema.tables)}
if counts != {'tables': 5, 'columns': 25, 'indexes': 12, 'foreign_keys': 4}:
    raise RuntimeError('Unexpected schema projection; review table layout before exporting.')
document.info.author = 'CommentsService'
document.info.caption = 'CommentsService database schema'
document.info.project = 'CommentsService'
document.info.version = version
document.info.description = 'Visualization projection; PostgreSQL migrations remain authoritative.'
diagram = model.addNewDiagram(0)
diagram.name = 'CommentsService'
positions = {'Authors': (40, 40), 'Comments': (440, 40), 'ImageAttachments': (940, 40),
    'TextAttachments': (940, 440), '__EFMigrationsHistory': (40, 440)}
for table in schema.tables:
    figure = diagram.placeTable(table, *positions[table.name])
    figure.name = table.name
    figure.color = '#DDEBFA'
    figure.manualSizing = 1
    figure.width = 320
    figure.height = 80 + 24 * len(table.columns)
if len(diagram.figures) != 5 or len(diagram.connections) != 4:
    raise RuntimeError('Incomplete Workbench diagram.')
grt.modules.Workbench.saveModelAs(str(root / 'docs/database/commentsservice.mwb'))
grt.modules.Workbench.exportDiagramToPng(diagram, str(root / 'docs/database/workbench-schema.png'))
print(json.dumps({'version': version, 'sql_errors': errors, 'counts': counts,
    'figures': len(diagram.figures), 'connections': len(diagram.connections)}))
