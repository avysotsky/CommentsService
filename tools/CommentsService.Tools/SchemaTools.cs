using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
namespace CommentsService.Tools;

internal static class SchemaTools
{
    private static string Text(JsonNode? node,string key)=>node![key]!.GetValue<string>();
    private static void Equal(IEnumerable<string> a,IEnumerable<string> b,string message)
    {if(!a.Order(StringComparer.Ordinal).SequenceEqual(b.Order(StringComparer.Ordinal)))throw new ToolError(message);}
    private static string Fields(string text)=>string.Join(",",Regex.Matches(text,@"`([^`]+)`\s*(DESC)?",RegexOptions.IgnoreCase).Select(m=>m.Groups[1].Value+":"+(m.Groups[2].Success?"desc":"asc")));
    private static string List(JsonNode? value)=>string.Join(",",value!.AsArray().Select(x=>x!.GetValue<string>()));
    public static async Task Verify(Options a)
    {
        var target=DatabaseTools.Target(a);await using var c=await DatabaseTools.Connect(target);
        var columns=await DatabaseTools.Rows(c,"SELECT json_agg(x) FROM (SELECT table_name,column_name,udt_name,character_maximum_length,is_nullable FROM information_schema.columns WHERE table_schema='comments' ORDER BY table_name,ordinal_position) x");
        var indexes=await DatabaseTools.Rows(c,"SELECT json_agg(x) FROM (SELECT tablename,indexdef FROM pg_indexes WHERE schemaname='comments' ORDER BY tablename,indexname) x");
        var fks=await DatabaseTools.Rows(c,"SELECT json_agg(x) FROM (SELECT t.relname AS table_name,r.relname AS target,c.confdeltype AS deletion,\nARRAY(SELECT a.attname FROM unnest(c.conkey) WITH ORDINALITY k(n,o) JOIN pg_attribute a ON a.attrelid=c.conrelid AND a.attnum=k.n ORDER BY k.o) AS columns,\nARRAY(SELECT a.attname FROM unnest(c.confkey) WITH ORDINALITY k(n,o) JOIN pg_attribute a ON a.attrelid=c.confrelid AND a.attnum=k.n ORDER BY k.o) AS referenced\nFROM pg_constraint c JOIN pg_class t ON t.oid=c.conrelid JOIN pg_class r ON r.oid=c.confrelid JOIN pg_namespace s ON s.oid=t.relnamespace WHERE s.nspname='comments' AND c.contype='f') x");
        var primary=await DatabaseTools.Rows(c,"SELECT json_agg(x) FROM (SELECT t.relname AS table_name,ARRAY(SELECT a.attname FROM unnest(c.conkey) WITH ORDINALITY k(n,o) JOIN pg_attribute a ON a.attrelid=c.conrelid AND a.attnum=k.n ORDER BY k.o) AS columns FROM pg_constraint c JOIN pg_class t ON t.oid=c.conrelid JOIN pg_namespace n ON n.oid=t.relnamespace WHERE n.nspname='comments' AND c.contype='p') x");
        var checks=await DatabaseTools.Rows(c,"SELECT json_agg(c.conname ORDER BY c.conname) FROM pg_constraint c JOIN pg_namespace n ON n.oid=c.connamespace WHERE n.nspname='comments' AND c.contype='c'");
        var history=await DatabaseTools.Rows(c,"SELECT json_agg(\"MigrationId\" ORDER BY \"MigrationId\") FROM comments.\"__EFMigrationsHistory\"");
        var wb=await File.ReadAllTextAsync("docs/database/workbench.sql");
        var blocks=Regex.Matches(wb,@"CREATE TABLE `([^`]+)`\s*\((.*?)\);",RegexOptions.Singleline).ToDictionary(m=>m.Groups[1].Value,m=>m.Groups[2].Value);
        Equal(blocks.Keys,columns.Select(x=>Text(x,"table_name")).Distinct(),"Workbench table set differs.");
        var expectedColumns=new List<string>();var expectedIndexes=new List<string>();var expectedPrimary=new List<string>();var expectedFks=new List<string>();
        foreach(var (table,body) in blocks)
        {
            foreach(var m in Regex.Matches(body,@"(?m)^\s*`([^`]+)`\s+(\w+(?:\(\d+(?:,\d+)?\))?)\s+(NOT NULL|NULL)").Cast<Match>())
                expectedColumns.Add(table+"|"+m.Groups[1].Value+"|"+m.Groups[2].Value.ToUpperInvariant()+"|"+(m.Groups[3].Value=="NULL"));
            foreach(var m in Regex.Matches(body,@"(PRIMARY KEY|UNIQUE KEY `[^`]+`|INDEX `[^`]+`)\s*\(([^)]+)\)").Cast<Match>())
            {
                expectedIndexes.Add(table+"|"+(m.Groups[1].Value.StartsWith("PRIMARY",StringComparison.Ordinal)||m.Groups[1].Value.StartsWith("UNIQUE",StringComparison.Ordinal))+"|"+Fields(m.Groups[2].Value));
                if(m.Groups[1].Value=="PRIMARY KEY")expectedPrimary.Add(table+"|"+string.Join(",",Regex.Matches(m.Groups[2].Value,@"`([^`]+)`").Select(x=>x.Groups[1].Value)));
            }
            foreach(var m in Regex.Matches(body,@"FOREIGN KEY\s*\(([^)]+)\)\s*REFERENCES `([^`]+)`\s*\(([^)]+)\) ON DELETE (RESTRICT|CASCADE)").Cast<Match>())
                expectedFks.Add(table+"|"+string.Join(",",Regex.Matches(m.Groups[1].Value,@"`([^`]+)`").Select(x=>x.Groups[1].Value))+"|"+m.Groups[2].Value+"|"+string.Join(",",Regex.Matches(m.Groups[3].Value,@"`([^`]+)`").Select(x=>x.Groups[1].Value))+"|"+m.Groups[4].Value);
        }
        var actualColumns=columns.Select(x=>
        {
            var type=Text(x,"udt_name");var length=x!["character_maximum_length"]?.GetValue<int>();
            var converted=type switch{"uuid"=>"CHAR(36)","inet"=>"VARCHAR(45)","timestamptz"=>"DATETIME(6)","int4"=>"INT","bytea"=>"MEDIUMBLOB","varchar"=>length==65536?"MEDIUMTEXT":$"VARCHAR({length})",_=>throw new ToolError("Unmapped SQL column type.")};
            return Text(x,"table_name")+"|"+Text(x,"column_name")+"|"+converted+"|"+(Text(x,"is_nullable")=="YES");
        });
        Equal(expectedColumns,actualColumns,"Workbench columns/types/nullability differ.");
        Equal(expectedIndexes,indexes.Select(x=>{var d=Text(x,"indexdef");var start=d.IndexOf("USING btree (",StringComparison.Ordinal);if(start<0)throw new ToolError("Unexpected index type.");return Text(x,"tablename")+"|"+d.StartsWith("CREATE UNIQUE INDEX",StringComparison.Ordinal)+"|"+Fields(d[(start+12)..^1].Replace('"','`'));}),"Workbench indexes differ.");
        Equal(expectedPrimary,primary.Select(x=>Text(x,"table_name")+"|"+List(x!["columns"])),"Workbench primary keys differ.");
        Equal(expectedFks,fks.Select(x=>Text(x,"table_name")+"|"+List(x!["columns"])+"|"+Text(x,"target")+"|"+List(x["referenced"])+"|"+(Text(x,"deletion") switch{"r"=>"RESTRICT","c"=>"CASCADE",_=>"UNKNOWN"})),"Workbench foreign keys differ.");
        var sql=await File.ReadAllTextAsync("docs/database/postgresql.sql");
        Equal(Regex.Matches(sql,"CONSTRAINT \"(CK_[^\"]+)\"").Select(x=>x.Groups[1].Value).Distinct(),checks.Select(x=>x!.GetValue<string>()),"SQL check constraints differ.");
        Equal(DatabaseTools.ExpectedMigrations(),history.Select(x=>x!.GetValue<string>()),"Migration history differs.");
        var report=JsonSerializer.Serialize(new{passed=true,tables=blocks.Count,columns=columns.Count,indexes=indexes.Count,foreign_keys=fks.Count,primary_keys=primary.Count,checks=checks.Count},new JsonSerializerOptions{WriteIndented=true});
        if(a.Has("output"))await File.WriteAllTextAsync(a.Require("output"),report);
        Console.WriteLine("PASS: SQL and Workbench projection agree on tables/columns/types/nullability/indexes/PK/FK/checks/history.");
    }
}
