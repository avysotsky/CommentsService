using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using CommentsService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;
namespace CommentsService.Tools;

internal sealed record DatabaseTarget(string Host,string Database,string Name,string Password,string AdminPassword);
internal static class DatabaseTools
{
    internal static DatabaseTarget Target(Options a)
    {
        var v=Secrets.Read(a.EnvFile);var target=a.Get("target","development");
        if(target is not ("development" or "test" or "benchmark"))throw new ToolError("Target must be development, test or benchmark.");
        var test=target!="development";
        return new(test?"test-database":"database",target=="benchmark"?"comments_benchmark":test?"comments_test":"comments",target=="benchmark"?"benchmark":test?"test-database":"database",Secrets.Need(v,test?"TEST_DB_APP_PASSWORD":"DB_APP_PASSWORD"),Secrets.Need(v,test?"TEST_DB_ADMIN_PASSWORD":"DB_ADMIN_PASSWORD"));
    }
    internal static async Task<NpgsqlConnection> Connect(DatabaseTarget t,string? database=null,bool admin=true)
    {
        var cs=new NpgsqlConnectionStringBuilder{Host=t.Host,Port=5432,Database=database??t.Database,Username=admin?"postgres":"comments_app",Password=admin?t.AdminPassword:t.Password,IncludeErrorDetail=false,Pooling=false,Timeout=5,CommandTimeout=300};
        var c=new NpgsqlConnection(cs.ConnectionString);await c.OpenAsync();return c;
    }
    internal static async Task<string> Scalar(NpgsqlConnection c,string sql)
    {await using var cmd=new NpgsqlCommand(sql,c);return (await cmd.ExecuteScalarAsync())?.ToString()??"";}
    internal static async Task Execute(NpgsqlConnection c,string sql)
    {await using var cmd=new NpgsqlCommand(sql,c);await cmd.ExecuteNonQueryAsync();}
    internal static async Task<JsonArray> Rows(NpgsqlConnection c,string sql) => JsonNode.Parse(await Scalar(c,sql))?.AsArray()??[];
    internal static string Quote(string name)=>"\""+name.Replace("\"","\"\"",StringComparison.Ordinal)+"\"";
    internal static string[] ExpectedMigrations()=>Directory.GetFiles("src/CommentsService.Infrastructure/Persistence/Migrations","*.cs").Select(Path.GetFileNameWithoutExtension).Where(n=>n is not null && char.IsAsciiDigit(n[0]) && !n.EndsWith(".Designer",StringComparison.Ordinal)).Cast<string>().Order(StringComparer.Ordinal).ToArray();
    internal static async Task<Dictionary<string,string>> Inventory(DatabaseTarget t,string database)
    {
        await using var c=await Connect(t,database);
        var tables=await Rows(c,"SELECT coalesce(json_agg(x ORDER BY x.s,x.t),'[]') FROM (SELECT schemaname s,tablename t FROM pg_tables WHERE schemaname NOT IN ('pg_catalog','information_schema')) x");
        var result=new Dictionary<string,string>(StringComparer.Ordinal);
        foreach(var row in tables)
        {
            var qualified=Quote(row!["s"]!.GetValue<string>())+"."+Quote(row["t"]!.GetValue<string>());
            result[qualified]=await Scalar(c,"SELECT count(*)::text || '|' || coalesce(md5(string_agg(h,'' ORDER BY h)),'empty') FROM (SELECT md5(to_jsonb(t)::text) h FROM "+qualified+" t) rows");
        }
        return result;
    }
    internal static bool Equal(Dictionary<string,string> a,Dictionary<string,string> b)=>a.Count==b.Count && a.All(p=>b.TryGetValue(p.Key,out var v)&&v==p.Value);
    private static Dictionary<string,string> PgEnv(DatabaseTarget t)=>new(){{"PGHOST",t.Host},{"PGPORT","5432"},{"PGUSER","postgres"},{"PGPASSWORD",t.AdminPassword}};
    internal static async Task DropOwned(DatabaseTarget t,string database)
    {
        if(!Regex.IsMatch(database,"^cs_tools_restore_[0-9a-f]{32}$",RegexOptions.CultureInvariant))throw new ToolError("Invalid recovery database ownership marker.");
        await using var c=await Connect(t,"postgres");
        await using var check=new NpgsqlCommand("SELECT pg_get_userbyid(datdba) FROM pg_database WHERE datname=@name",c);check.Parameters.AddWithValue("name",database);
        if((await check.ExecuteScalarAsync())?.ToString()!="comments_app")throw new ToolError("Recovery database owner changed; cleanup refused.");
        await Execute(c,"DROP DATABASE "+Quote(database));
    }
    private static async Task Restore(DatabaseTarget t,string name,string dump)
    {
        await using var stream=File.OpenRead(dump);
        await Processes.Run("pg_restore",["--exit-on-error","--no-owner","--no-privileges","--role=comments_app","-d",name],PgEnv(t),stream,Stream.Null);
    }
    public static async Task MigrateOrCheck(Options a)
    {
        var t=Target(a);Environment.SetEnvironmentVariable("PGHOST",t.Host);Environment.SetEnvironmentVariable("PGPORT","5432");Environment.SetEnvironmentVariable("PGDATABASE",t.Database);Environment.SetEnvironmentVariable("PGUSER","comments_app");Environment.SetEnvironmentVariable("PGPASSWORD",t.Password);
        await using var db=new CommentsDbContextFactory().CreateDbContext([]);
        if(a.Command=="migrate")await db.Database.MigrateAsync();
        var applied=(await db.Database.GetAppliedMigrationsAsync()).ToArray();
        if(!applied.SequenceEqual(ExpectedMigrations()))throw new ToolError("Database migrations are not current; run an explicit migrate after preserving recovery copies.");
        Console.WriteLine("PASS: application-role database migrations are current for "+t.Database);
    }
    public static async Task Checkpoint(Options a)
    {
        var primary=Target(a);var v=Secrets.Read(a.EnvFile);
        var targets=new List<DatabaseTarget>{primary};
        if(!a.Has("target"))targets.Add(new("test-database","comments_test","test-database",Secrets.Need(v,"TEST_DB_APP_PASSWORD"),Secrets.Need(v,"TEST_DB_ADMIN_PASSWORD")));
        if(a.Has("include-benchmark") && targets.All(t=>t.Database!="comments_benchmark"))targets.Add(new("test-database","comments_benchmark","benchmark",Secrets.Need(v,"TEST_DB_APP_PASSWORD"),Secrets.Need(v,"TEST_DB_ADMIN_PASSWORD")));
        var output=a.Get("output","artifacts/checkpoints");
        if(OperatingSystem.IsWindows())Directory.CreateDirectory(output);else Directory.CreateDirectory(output,UnixFileMode.UserRead|UnixFileMode.UserWrite|UnixFileMode.UserExecute);
        foreach(var t in targets)
        {
            var id=Guid.NewGuid().ToString("N");var temp="cs_tools_restore_"+id;var candidate=Path.Combine(output,t.Name+".candidate-"+id+".dump");var created=false;
            try
            {
                var before=await Inventory(t,t.Database);
                await using(var f=Secrets.CreatePrivate(candidate))await Processes.Run("pg_dump",["-d",t.Database,"-Fc"],PgEnv(t),output:f);
                await using(var c=await Connect(t,"postgres"))await Execute(c,"CREATE DATABASE "+Quote(temp)+" OWNER comments_app");created=true;
                await Restore(t,temp,candidate);
                var restored=await Inventory(t,temp);var after=await Inventory(t,t.Database);
                if(!Equal(before,restored)||!Equal(before,after))throw new ToolError("Checkpoint verification mismatch; previous recovery point preserved.");
                await using var source=File.OpenRead(candidate);var sha=Convert.ToHexString(await SHA256.HashDataAsync(source)).ToLowerInvariant();source.Close();
                var metadata=new{database=t.Database,created_at_utc=DateTimeOffset.UtcNow,tables=restored,sha256=sha,restore_verified=true};
                var meta=candidate+".json";using(var writer=new StreamWriter(Secrets.CreatePrivate(meta)))await writer.WriteAsync(JsonSerializer.Serialize(metadata,new JsonSerializerOptions{WriteIndented=true}));
                File.Move(candidate,Path.Combine(output,t.Name+".dump"),true);File.Move(meta,Path.Combine(output,t.Name+".json"),true);
                Console.WriteLine("PASS: checkpoint restored and row fingerprints verified for "+t.Database);
            }
            finally{if(created)await DropOwned(t,temp);}
        }
    }
    public static async Task VerifyCheckpoint(Options a)
    {
        var t=Target(a);var dump=a.Require("dump");var manifest=a.Get("manifest",Path.ChangeExtension(dump,".json"));
        var meta=JsonNode.Parse(await File.ReadAllTextAsync(manifest))!.AsObject();
        async Task<string> Hash(){await using var f=File.OpenRead(dump);return Convert.ToHexString(await SHA256.HashDataAsync(f)).ToLowerInvariant();}
        var hash=await Hash();if(hash!=meta["sha256"]!.GetValue<string>())throw new ToolError("Checkpoint SHA256 mismatch; no recovery database created.");
        var expected=meta["tables"]!.Deserialize<Dictionary<string,string>>()!;var name="cs_tools_restore_"+Guid.NewGuid().ToString("N");var created=false;
        try
        {
            await using(var c=await Connect(t,"postgres"))await Execute(c,"CREATE DATABASE "+Quote(name)+" OWNER comments_app");created=true;
            await Restore(t,name,dump);var before=await Inventory(t,name);if(!Equal(before,expected))throw new ToolError("Restored tables differ from checkpoint manifest.");
            if(a.Has("upgrade"))
            {
                await using var c=await Connect(t,name);
                var sql="SET ROLE comments_app;\n"+await File.ReadAllTextAsync("docs/database/postgresql.sql");
                await Execute(c,sql);var after=await Inventory(t,name);
                if(before.Where(p=>p.Key!="\"comments\".\"__EFMigrationsHistory\"").Any(p=>!after.TryGetValue(p.Key,out var x)||x!=p.Value))throw new ToolError("Upgrade changed existing data.");
                var history=await Rows(c,"SELECT json_agg(\"MigrationId\" ORDER BY \"MigrationId\") FROM comments.\"__EFMigrationsHistory\"");
                if(!history.Select(x=>x!.GetValue<string>()).SequenceEqual(ExpectedMigrations()))throw new ToolError("Upgrade history incomplete.");
                await Execute(c,sql);if(!Equal(after,await Inventory(t,name)))throw new ToolError("Repeated schema export changed data.");
            }
            await using(var c=await Connect(t,name))if(await Scalar(c,"SELECT count(*) FROM pg_class t JOIN pg_namespace n ON n.oid=t.relnamespace WHERE n.nspname='comments' AND t.relkind='r' AND pg_get_userbyid(t.relowner)<>'comments_app'")!="0")throw new ToolError("Unexpected restored table ownership.");
            if(await Hash()!=hash)throw new ToolError("Source checkpoint changed.");
            Console.WriteLine("PASS: checkpoint restored; original unchanged; optional upgrade/idempotency preserved existing data.");
        }
        finally{if(created)await DropOwned(t,name);}
    }
    public static async Task Tests(Options a)
    {
        var t=Target(a);if(t.Database!="comments_test")throw new ToolError("Backend tests require --target test.");
        var env=new Dictionary<string,string>{{"PGHOST",t.Host},{"PGPORT","5432"},{"PGDATABASE",t.Database},{"PGUSER","comments_app"},{"PGPASSWORD",t.Password}};
        foreach(var project in new[]{"Domain","Application","Data","Api"})await Processes.Run("dotnet",["test","tests/CommentsService."+project+".Tests","-c","Release"],env);
    }
}
