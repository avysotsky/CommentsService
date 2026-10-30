using CommentsService.Application.Comments.CreateComment;
using CommentsService.Application.Images;
namespace CommentsService.Api.Contracts;

internal static class CreateCommentRequest
{
    public static async Task<CreateCommentInput?> ReadAsync(HttpContext context, CancellationToken token)
    {
        if (context.Request.HasJsonContentType())
        {
            var body = await JsonRequest.ReadAsync<CreateCommentBody>(context, token);
            return body is null ? null : new(body.UserName, body.Email, body.HomePage, body.Text, body.ParentId, body.CaptchaId, body.CaptchaCode);
        }
        IFormCollection form;
        try { form = await context.Request.ReadFormAsync(token); }
        catch (InvalidDataException) { throw new BadHttpRequestException("Invalid multipart body."); }
        var allowed = new HashSet<string>(["userName", "email", "homePage", "text", "parentId", "captchaId", "captchaCode"], StringComparer.Ordinal);
        if (form.Any(pair => !allowed.Contains(pair.Key) || pair.Value.Count != 1) || form.Files.Count > 1 || form.Files.Any(file => file.Name is not ("image" or "textFile")))
            throw new BadHttpRequestException("Invalid form fields.");
        Guid? Id(string field)
        {
            var value = form[field].ToString();
            if (string.IsNullOrEmpty(value)) return null;
            return Guid.TryParse(value, out var id) ? id : throw new BadHttpRequestException("Invalid identifier.");
        }
        var file = form.Files.SingleOrDefault();
        return new(form["userName"], form["email"], form["homePage"], form["text"], Id("parentId"), Id("captchaId"), form["captchaCode"],
            file?.Name == "image" ? new ImageUpload(file.FileName, file.Length, file.OpenReadStream()) : null,
            file?.Name == "textFile" ? new CommentsService.Application.TextFiles.TextFileUpload(file.FileName, file.Length, file.OpenReadStream()) : null);
    }
}
