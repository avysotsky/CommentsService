namespace CommentsService.Api.Tests;
internal sealed class AcceptCaptcha : CommentsService.Application.Captcha.ICaptchaVerifier
{ public bool Verify(Guid? id, string? code) => true; }
