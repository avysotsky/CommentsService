namespace CommentsService.Infrastructure.Caching;

public sealed class CacheSettings
{
    public bool Enabled { get; set; } = true;
    public int LifetimeSeconds { get; set; } = 15;
    public int Capacity { get; set; } = 128;
    public long MaximumBytes { get; set; } = 16777216;

    public void Validate()
    {
        if (LifetimeSeconds is < 1 or > 300 || Capacity is < 1 or > 4096 || MaximumBytes is < 1024 or > 268435456)
            throw new InvalidOperationException("Invalid read cache settings.");
    }
}
