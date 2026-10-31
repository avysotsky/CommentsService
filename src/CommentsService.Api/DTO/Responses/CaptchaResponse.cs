namespace CommentsService.Api.DTO.Responses;

public sealed record CaptchaResponse(Guid Id, string ImageDataUrl, DateTimeOffset ExpiresAt);
