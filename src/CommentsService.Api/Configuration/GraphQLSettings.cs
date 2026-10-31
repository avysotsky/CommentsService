namespace CommentsService.Api.Configuration;

public sealed class GraphQLSettings
{
    public bool Enabled { get; set; } = true;
    public int MaximumRequestBytes { get; set; } = 32768;
    public int MaximumDepth { get; set; } = 8;
    public int MaximumFields { get; set; } = 256;
    public int MaximumFieldCost { get; set; } = 1000;
    public int MaximumTypeCost { get; set; } = 2000;
    public int ExecutionTimeoutSeconds { get; set; } = 10;
    public int MaximumConcurrentRequests { get; set; } = 16;

    public void Validate()
    {
        if (MaximumRequestBytes is < 256 or > 1048576 || MaximumDepth is < 2 or > 16 ||
            MaximumFields is < 16 or > 1024 || MaximumFieldCost is < 1 or > 10000 ||
            MaximumTypeCost is < 1 or > 100000 || ExecutionTimeoutSeconds is < 1 or > 30 ||
            MaximumConcurrentRequests is < 1 or > 64)
            throw new InvalidOperationException("Invalid GraphQL settings.");
    }
}
