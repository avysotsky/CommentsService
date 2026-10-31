namespace CommentsService.Application.Captcha;

public sealed record CaptchaChallenge(Guid Id, byte[] Image, DateTimeOffset ExpiresAt);
