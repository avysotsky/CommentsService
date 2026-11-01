using System.Security.Cryptography;
using System.Text;
namespace CommentsService.Tools;

internal static class Secrets
{
    public static Dictionary<string,string> Read(string path)
    {
        var result=new Dictionary<string,string>(StringComparer.Ordinal);
        foreach(var raw in File.ReadAllLines(path))
        {
            var line=raw.Trim(); if(line.Length==0 || line.StartsWith('#'))continue;
            var i=line.IndexOf('='); if(i<1)throw new ToolError("Invalid private environment file format.");
            var value=line[(i+1)..].Trim();
            if(value.Length>=2 && ((value[0]=='\"' && value[^1]=='\"') || (value[0]=='\'' && value[^1]=='\''))) value=value[1..^1];
            result[line[..i].Trim()]=value;
        }
        return result;
    }
    public static string Need(Dictionary<string,string> values,string key) => values.TryGetValue(key,out var value) && !string.IsNullOrWhiteSpace(value)?value:throw new ToolError("Missing private environment setting: "+key);
    public static void WriteNew(string path,Dictionary<string,string> values)
    {
        var parent=Path.GetDirectoryName(Path.GetFullPath(path))!;Directory.CreateDirectory(parent);
        if(File.Exists(path))
        {
            var existing=Read(path);
            if(values.Keys.Any(k=>!existing.ContainsKey(k)))throw new ToolError("Existing environment is incomplete and was not changed.");
            Console.WriteLine("Existing private environment preserved.");return;
        }
        using var stream=CreatePrivate(path);using var writer=new StreamWriter(stream,new UTF8Encoding(false));
        foreach(var pair in values)writer.WriteLine(pair.Key+"="+pair.Value);
        Console.WriteLine("Private environment created; values were not printed.");
    }
    internal static FileStream CreatePrivate(string path)
    {
        var options=new FileStreamOptions{Mode=FileMode.CreateNew,Access=FileAccess.Write,Share=FileShare.None};
        if(!OperatingSystem.IsWindows())options.UnixCreateMode=UnixFileMode.UserRead|UnixFileMode.UserWrite;
        return new FileStream(path,options);
    }
    private static string Random() => Convert.ToHexString(RandomNumberGenerator.GetBytes(24)).ToLowerInvariant();
    public static void Init(Options a,string kind)
    {
        if(kind=="main")
        {
            var path=a.Get("output",".env");
            var data=new Dictionary<string,string>{{"DB_ADMIN_PASSWORD",Random()},{"DB_APP_PASSWORD",Random()},{"TEST_DB_ADMIN_PASSWORD",Random()},{"TEST_DB_APP_PASSWORD",Random()},{"APP_PORT",a.Number("port",8080).ToString()},{"TEST_DB_PORT",a.Number("test-db-port",55434).ToString()}};
            if(File.Exists(path)){var old=Read(path);foreach(var k in data.Keys.Where(k=>k.EndsWith("PASSWORD",StringComparison.Ordinal)))_=Need(old,k);}
            WriteNew(path,data);
        }
        else if(kind=="broker")
        {
            var path=a.Get("output",".env.broker");var password=Random();
            if(File.Exists(path)){var old=Read(path);if(Need(old,"BROKER_PASSWORD")!=Need(old,"RABBITMQ_DEFAULT_PASS") || Need(old,"Runtime__Broker__Username")!=Need(old,"RABBITMQ_DEFAULT_USER"))throw new ToolError("Existing broker credentials differ; file preserved.");}
            WriteNew(path,new(){{"Runtime__Broker__Enabled","true"},{"Runtime__Broker__Host","broker"},{"Runtime__Broker__Username","comments_app"},{"BROKER_PASSWORD",password},{"RABBITMQ_DEFAULT_USER","comments_app"},{"RABBITMQ_DEFAULT_PASS",password}});
        }
        else if(kind=="search")
        {
            var app=a.Get("app-env",".env.search");var server=a.Get("server-env",".env.elasticsearch");if(Path.GetFullPath(app)==Path.GetFullPath(server))throw new ToolError("Use separate app and server environment files.");
            if(File.Exists(app))_=Need(Read(app),"SEARCH_PASSWORD");if(File.Exists(server))_=Need(Read(server),"ELASTIC_PASSWORD");
            WriteNew(app,new(){{"Runtime__Search__Enabled","true"},{"Runtime__Search__Endpoint","http://elasticsearch:9200"},{"Runtime__Search__Username","comments_search"},{"SEARCH_PASSWORD",Random()}});
            WriteNew(server,new(){{"ELASTIC_PASSWORD",Random()}});
        }
        else WriteNew(a.Get("output",".env.cloud"),new(){{"SUPABASE_URL",""},{"SUPABASE_SECRET_KEY",""},{"SUPABASE_BUCKET","comments-attachments"}});
    }
}
