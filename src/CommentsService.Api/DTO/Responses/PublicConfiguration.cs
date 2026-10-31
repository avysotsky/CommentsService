namespace CommentsService.Api.DTO.Responses;

// Explicit allowlist: never serialize RuntimeSettings or database/logging/proxy settings.
public sealed record PublicConfiguration(int RootPageSize, int DefaultListLimit, int MaximumListLimit,
    int UserNameMaxLength, int EmailMaxLength, int HomePageMaxLength, int TextMaxLength,
    int ImageUploadMaxBytes, int ImageMaxWidth, int ImageMaxHeight, int TextFileMaxBytes,
    int CaptchaCodeLength, string CaptchaAlphabet, int CaptchaLifetimeSeconds,
    bool GraphQLEnabled, bool RealtimeEnabled, int ReconnectDelayMilliseconds, int MaximumReconnectDelayMilliseconds);
