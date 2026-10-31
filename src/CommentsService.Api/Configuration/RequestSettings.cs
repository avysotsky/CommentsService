namespace CommentsService.Api.Configuration;

public sealed class RequestSettings
{
    public int JsonMaxBytes { get; set; } = 1048576;
    public int JsonMaxDepth { get; set; } = 16;
    public int BodyMaxBytes { get; set; } = 6291456;
    public int MultipartMaxBytes { get; set; } = 5242880;
    public int FormValueMaxLength { get; set; } = 262144;
    public int FormValueCountLimit { get; set; } = 16;
    public int HealthTimeoutSeconds { get; set; } = 2;

    public void Validate()
    {
        if (JsonMaxBytes is < 1 or > 1048576 || JsonMaxDepth is < 1 or > 16 || BodyMaxBytes is < 1 or > 6291456 ||
            MultipartMaxBytes is < 1 or > 5242880 || MultipartMaxBytes > BodyMaxBytes || JsonMaxBytes > BodyMaxBytes ||
            FormValueMaxLength is < 1 or > 262144 || FormValueCountLimit is < 8 or > 16 || HealthTimeoutSeconds is < 1 or > 60)
            throw new InvalidOperationException("Invalid request limits.");
    }
}
