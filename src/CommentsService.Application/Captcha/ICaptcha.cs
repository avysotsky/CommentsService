namespace CommentsService.Application.Captcha;

public interface ICaptchaIssuer { CaptchaChallenge Issue(Guid? replace = null); }
public interface ICaptchaVerifier { bool Verify(Guid? id, string? code); }
public sealed class CaptchaCapacityException : Exception { }
