using System.Diagnostics;
namespace CommentsService.Tools;
internal static class Processes
{
    public static async Task Run(string file,IEnumerable<string> args,Dictionary<string,string>? env=null,Stream? input=null,Stream? output=null)
    {
        var start=new ProcessStartInfo(file){UseShellExecute=false,RedirectStandardInput=input is not null,RedirectStandardOutput=output is not null,RedirectStandardError=true};
        foreach(var a in args)start.ArgumentList.Add(a);if(env is not null)foreach(var p in env)start.Environment[p.Key]=p.Value;
        using var process=Process.Start(start)??throw new ToolError("Could not start maintenance client.");
        var error=process.StandardError.ReadToEndAsync();
        var read=output is null?Task.CompletedTask:process.StandardOutput.BaseStream.CopyToAsync(output);
        var write=Task.CompletedTask;
        if(input is not null)write=Feed();
        async Task Feed(){await input!.CopyToAsync(process.StandardInput.BaseStream);process.StandardInput.Close();}
        await Task.WhenAll(read,write,process.WaitForExitAsync());_=await error;
        if(process.ExitCode!=0)throw new ToolError("Maintenance client failed; original files were preserved. Check private settings and selected services.");
    }
}
