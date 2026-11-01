namespace CommentsService.Tools;

internal sealed class ToolError(string message) : Exception(message);
internal sealed class Options
{
    private readonly Dictionary<string,List<string>> values = new(StringComparer.Ordinal);
    private static readonly HashSet<string> Flags = ["include-benchmark", "upgrade"];
    private static readonly HashSet<string> Names = ["env-file","target","output","port","test-db-port","app-env","server-env","dump","manifest","base-url","database","expected-author","comment-id","include-benchmark","upgrade"];
    public string Command { get; }
    public Options(string[] args)
    {
        Command = args.FirstOrDefault() ?? "help";
        for (var i=1;i<args.Length;i++)
        {
            var name=args[i];
            if (!name.StartsWith("--",StringComparison.Ordinal) || !Names.Contains(name[2..])) throw new ToolError("Unknown option; use documented options.");
            name=name[2..]; var value="true";
            if (!Flags.Contains(name)) { if(++i>=args.Length || args[i].StartsWith("--",StringComparison.Ordinal)) throw new ToolError("Missing option value."); value=args[i]; }
            if (!values.TryGetValue(name,out var list)) values[name]=list=[];
            if(name!="comment-id" && list.Count!=0) throw new ToolError("Duplicate option.");
            list.Add(value);
        }
    }
    public string Get(string key,string fallback) => values.TryGetValue(key,out var v)?v[0]:fallback;
    public string Require(string key) => values.TryGetValue(key,out var v)?v[0]:throw new ToolError("Required option missing: --"+key);
    public string[] All(string key) => values.TryGetValue(key,out var v)?v.ToArray():[];
    public bool Has(string key) => values.ContainsKey(key);
    public int Number(string key,int fallback) => int.TryParse(Get(key,fallback.ToString()),out var n) && n is >0 and <=65535?n:throw new ToolError("Invalid port.");
    public string EnvFile => Get("env-file",".env");
}
