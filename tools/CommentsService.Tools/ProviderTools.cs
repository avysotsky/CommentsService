using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
namespace CommentsService.Tools;

internal static class ProviderTools
{
    private static HttpClient Client()=>new(new SocketsHttpHandler{AllowAutoRedirect=false,MaxConnectionsPerServer=2,ConnectTimeout=TimeSpan.FromSeconds(2)}){Timeout=Timeout.InfiniteTimeSpan};
    public static async Task ConfigureSearch(Options a)
    {
        var app=Secrets.Read(a.Get("app-env",".env.search"));var server=Secrets.Read(a.Get("server-env",".env.elasticsearch"));
        if(app.GetValueOrDefault("Runtime__Search__Username","comments_search")!="comments_search")throw new ToolError("Only the scoped comments_search user can be managed.");
        var password=Secrets.Need(app,"SEARCH_PASSWORD");var admin=Secrets.Need(server,"ELASTIC_PASSWORD");
        using var client=Client();
        foreach(var (path,body) in new (string,object)[]{("role/comments_search",new{cluster=Array.Empty<string>(),indices=new[]{new{names=new[]{"comments-*"},privileges=new[]{"read","write","create_index","view_index_metadata","monitor"}}}}),("user/comments_search",new{password,roles=new[]{"comments_search"}})})
        {
            using var deadline=new CancellationTokenSource(TimeSpan.FromSeconds(120));var complete=false;
            while(!deadline.IsCancellationRequested)
            {
                var retry=false;
                try
                {
                    using var timeout=CancellationTokenSource.CreateLinkedTokenSource(deadline.Token);timeout.CancelAfter(TimeSpan.FromSeconds(5));
                    using var request=new HttpRequestMessage(HttpMethod.Put,"http://elasticsearch:9200/_security/"+path);
                    request.Headers.Authorization=new AuthenticationHeaderValue("Basic",Convert.ToBase64String(Encoding.UTF8.GetBytes("elastic:"+admin)));
                    request.Content=new StringContent(JsonSerializer.Serialize(body),Encoding.UTF8,"application/json");
                    using var response=await client.SendAsync(request,HttpCompletionOption.ResponseHeadersRead,timeout.Token);
                    if(response.StatusCode is HttpStatusCode.OK or HttpStatusCode.Created){complete=true;break;}
                    retry=response.StatusCode is HttpStatusCode.RequestTimeout or HttpStatusCode.TooManyRequests || (int)response.StatusCode>=500;
                }
                catch(Exception e) when(e is HttpRequestException or OperationCanceledException){retry=true;}
                if(!retry)throw new ToolError("Search provisioning refused; check private credentials. Files were preserved.");
                try{await Task.Delay(2000,deadline.Token);}catch(OperationCanceledException){break;}
            }
            if(!complete)throw new ToolError("Search security readiness deadline expired; files were preserved.");
        }
        Console.WriteLine("PASS: scoped search role/user ready; admin credentials are not supplied to app.");
    }
    public static async Task VerifyCloud(Options a)
    {
        var origin=a.Get("base-url","http://app:8080");
        if(!Uri.TryCreate(origin,UriKind.Absolute,out var baseUri)||baseUri.Scheme is not ("http" or "https")||baseUri.Host is not ("app" or "localhost" or "127.0.0.1")||baseUri.UserInfo!=""||baseUri.Query!=""||baseUri.Fragment!=""||baseUri.AbsolutePath!="/")throw new ToolError("Use the local app service origin.");
        var ids=a.All("comment-id");if(ids.Length!=2||ids.Distinct().Count()!=2||ids.Any(x=>!Guid.TryParse(x,out _)))throw new ToolError("Select exactly two different own comment UUIDs.");
        var author=a.Require("expected-author");var values=Secrets.Read(a.Get("env-file",".env.cloud"));
        var url=Secrets.Need(values,"SUPABASE_URL").TrimEnd('/');var key=Secrets.Need(values,"SUPABASE_SECRET_KEY");var bucket=Secrets.Need(values,"SUPABASE_BUCKET");
        if(!Regex.IsMatch(url,@"^https://[a-z0-9-]+\.supabase\.co$",RegexOptions.CultureInvariant)||!Regex.IsMatch(key,@"^sb_secret_[A-Za-z0-9_-]+$",RegexOptions.CultureInvariant)||!Regex.IsMatch(bucket,@"^[a-z0-9][a-z0-9-]{0,62}$",RegexOptions.CultureInvariant))throw new ToolError("Invalid private Cloud configuration.");
        using var client=Client();var remaining=2*1024*1024;
        async Task<(HttpStatusCode Status,byte[] Data)> Read(string address,int maximum,string? secret=null)
        {
            using var timeout=new CancellationTokenSource(TimeSpan.FromSeconds(5));using var request=new HttpRequestMessage(HttpMethod.Get,address);
            if(secret is not null)request.Headers.Add("apikey",secret);
            using var response=await client.SendAsync(request,HttpCompletionOption.ResponseHeadersRead,timeout.Token);
            if(!response.IsSuccessStatusCode)return(response.StatusCode,[]);
            var limit=Math.Min(maximum,remaining);await using var stream=await response.Content.ReadAsStreamAsync(timeout.Token);
            var bytes=new byte[limit+1];var count=0;
            while(count<bytes.Length){var n=await stream.ReadAsync(bytes.AsMemory(count),timeout.Token);if(n==0)break;count+=n;}
            if(count>limit)throw new ToolError("Cloud demo response exceeded its budget.");remaining-=count;return(response.StatusCode,bytes[..count]);
        }
        var meta=await Read(url+"/storage/v1/bucket/"+bucket,16384,key);if(meta.Status!=HttpStatusCode.OK)throw new ToolError("Private bucket unavailable.");
        var metadata=JsonNode.Parse(meta.Data)!;
        if(metadata["public"]?.GetValue<bool>()!=false||!(metadata["allowed_mime_types"]?.AsArray().Select(x=>x!.GetValue<string>()).Order().SequenceEqual(new[]{"image/png","text/plain"})??false))throw new ToolError("Private bucket policy differs.");
        var partition=Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(a.Get("database","comments")))).ToLowerInvariant()[..12];
        var files=new List<object>();var kinds=new HashSet<string>();
        foreach(var value in ids)
        {
            var id=Guid.Parse(value).ToString();var comment=await Read(origin.TrimEnd('/')+"/api/comments/"+id,131072);
            if(comment.Status!=HttpStatusCode.OK)throw new ToolError("Own demo comment unavailable.");var node=JsonNode.Parse(comment.Data)!;
            if(node["id"]?.GetValue<string>()!=id||node["userName"]?.GetValue<string>()!=author)throw new ToolError("Own demo author marker differs.");
            var image=node["image"];var text=node["textFile"];if((image is null)==(text is null))throw new ToolError("Each demo comment must contain one file.");
            var fileId=Guid.Parse((image??text)!["id"]!.GetValue<string>());var kind=image is null?"text":"images";kinds.Add(kind);
            var original=await Read(origin.TrimEnd('/')+(image is null?"/api/text-files/":"/api/images/")+fileId,image is null?102400:524288);
            if(original.Status!=HttpStatusCode.OK||original.Data.Length==0)throw new ToolError("Own attachment unavailable.");
            var sha=Convert.ToHexString(SHA256.HashData(original.Data)).ToLowerInvariant();var objectKey=$"v1/{partition}/{kind}/{fileId:N}/{sha}.{(image is null?"txt":"png")}";
            var cloud=await Read(url+"/storage/v1/object/authenticated/"+bucket+"/"+objectKey,original.Data.Length,key);
            if(cloud.Status!=HttpStatusCode.OK||!cloud.Data.SequenceEqual(original.Data))throw new ToolError("Real Cloud bytes differ or object is missing.");
            var publicRead=await Read(url+"/storage/v1/object/public/"+bucket+"/"+objectKey,8192);
            if(publicRead.Status is not (HttpStatusCode.BadRequest or HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden or HttpStatusCode.NotFound))throw new ToolError("Cloud public access was not denied.");
            files.Add(new{commentId=id,attachmentId=fileId,objectKey,bytes=original.Data.Length,sha256=sha});
        }
        if(kinds.Count!=2)throw new ToolError("Select one own PNG and one own TXT.");
        Console.WriteLine(JsonSerializer.Serialize(new{passed=true,realCloud=true,readOnly=true,publicAccessDenied=true,files},new JsonSerializerOptions{WriteIndented=true}));
    }
}
