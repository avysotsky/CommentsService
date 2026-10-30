namespace CommentsService.Application.Captcha;

public sealed record CaptchaChallenge(Guid Id, byte[] Image, DateTimeOffset ExpiresAt);
public interface ICaptchaIssuer { CaptchaChallenge Issue(Guid? replace = null); }
public interface ICaptchaVerifier { bool Verify(Guid? id, string? code); }
public sealed class CaptchaCapacityException : Exception { }
