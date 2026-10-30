using CommentsService.Application.Comments.CreateComment;
using CommentsService.Application.Images;
using Microsoft.AspNetCore.Http.Features;
namespace CommentsService.Api.Contracts;

internal static class CreateCommentRequest
{
    public static async Task<CreateCommentInput?> ReadAsync(HttpContext context, CancellationToken token)
    {
        if (context.Request.HasJsonContentType())
        {
            if (context.Features.Get<IHttpMaxRequestBodySizeFeature>() is { IsReadOnly: false } size) size.MaxRequestBodySize = 1024 * 1024;
            if (context.Request.ContentLength > 1024 * 1024) throw new BadHttpRequestException("Request too large.", 413);
            var body = await context.Request.ReadFromJsonAsync<CreateCommentBody>(token);
            return body is null ? null : new(body.UserName, body.Email, body.HomePage, body.Text, body.ParentId, body.CaptchaId, body.CaptchaCode);
        }
        IFormCollection form;
        try { form = await context.Request.ReadFormAsync(token); }
        catch (InvalidDataException) { throw new BadHttpRequestException("Invalid multipart body."); }
        var allowed = new HashSet<string>(["userName", "email", "homePage", "text", "parentId", "captchaId", "captchaCode"], StringComparer.Ordinal);
        if (form.Any(pair => !allowed.Contains(pair.Key) || pair.Value.Count != 1) || form.Files.Count > 1 || form.Files.Any(file => file.Name != "image"))
            throw new BadHttpRequestException("Invalid form fields.");
        Guid? Id(string field)
        {
            var value = form[field].ToString();
            if (string.IsNullOrEmpty(value)) return null;
            return Guid.TryParse(value, out var id) ? id : throw new BadHttpRequestException("Invalid identifier.");
        }
        var file = form.Files.SingleOrDefault();
        return new(form["userName"], form["email"], form["homePage"], form["text"], Id("parentId"), Id("captchaId"), form["captchaCode"],
            file is null ? null : new ImageUpload(file.FileName, file.Length, file.OpenReadStream()));
    }
}
