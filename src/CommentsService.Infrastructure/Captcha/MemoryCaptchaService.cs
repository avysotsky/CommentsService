using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using CommentsService.Application.Captcha;

namespace CommentsService.Infrastructure.Captcha;

public interface ICaptchaImageRenderer { byte[] Render(string code); }

// Process-local, bounded, expiring and atomic: every verification attempt consumes its ID.
public sealed class MemoryCaptchaService(TimeProvider clock, ICaptchaImageRenderer renderer, CaptchaSettings? settings = null) : ICaptchaIssuer, ICaptchaVerifier
{
    private readonly CaptchaSettings policy = settings ?? new();
    public const string Alphabet = "23456789ABCDEFGHJKLMNPQRSTUVWXYZ";
    private sealed record Entry(byte[] Hash, DateTimeOffset ExpiresAt);
    private readonly ConcurrentDictionary<Guid, Entry> entries = new();
    private readonly object gate = new();
    public CaptchaChallenge Issue(Guid? replace = null)
    {
        lock (gate)
        {
            var now = clock.GetUtcNow();
            foreach (var pair in entries) if (pair.Value.ExpiresAt <= now) entries.TryRemove(pair.Key, out _);
            if (replace is { } old) entries.TryRemove(old, out _);
            if (entries.Count >= policy.Capacity) throw new CaptchaCapacityException();
            var code = new string(Enumerable.Range(0, policy.CodeLength).Select(_ => policy.Alphabet[RandomNumberGenerator.GetInt32(policy.Alphabet.Length)]).ToArray());
            var image = renderer.Render(code);
            var id = Guid.NewGuid(); var expiry = now + TimeSpan.FromSeconds(policy.LifetimeSeconds);
            entries[id] = new(SHA256.HashData(Encoding.ASCII.GetBytes(code)), expiry);
            return new(id, image, expiry);
        }
    }
    public bool Verify(Guid? id, string? code)
    {
        if (id is null || !entries.TryRemove(id.Value, out var entry)) return false;
        if (entry.ExpiresAt <= clock.GetUtcNow() || code is null) return false;
        var normalized = code.Trim().ToUpperInvariant();
        if (normalized.Length != policy.CodeLength || normalized.Any(c => !policy.Alphabet.Contains(c))) return false;
        return CryptographicOperations.FixedTimeEquals(entry.Hash, SHA256.HashData(Encoding.ASCII.GetBytes(normalized)));
    }
}
