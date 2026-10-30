using System.Buffers.Binary;
using System.IO.Compression;
using CommentsService.Application.Captcha;
using CommentsService.Infrastructure.Captcha;
using Xunit;
namespace CommentsService.Data.Tests;

public sealed class CaptchaTests
{
    private sealed class Clock : TimeProvider
    { public DateTimeOffset Now = DateTimeOffset.Parse("2026-10-30T00:00:00Z"); public override DateTimeOffset GetUtcNow() => Now; }
    private sealed class Renderer : ICaptchaImageRenderer
    { public string Code = ""; public byte[] Render(string code) { Code = code; return [1]; } }
    [Fact]
    public void CorrectCodeIsCaseInsensitiveAndSingleUse()
    {
        var renderer = new Renderer(); var clock = new Clock(); var service = new MemoryCaptchaService(clock, renderer);
        var issued = service.Issue(); Assert.Equal(clock.Now.AddMinutes(5), issued.ExpiresAt);
        Assert.Equal(6, renderer.Code.Length); Assert.All(renderer.Code, c => Assert.Contains(c, MemoryCaptchaService.Alphabet));
        Assert.True(service.Verify(issued.Id, " " + renderer.Code.ToLowerInvariant() + " "));
        Assert.False(service.Verify(issued.Id, renderer.Code));
    }
    [Fact]
    public void WrongAttemptsAndRefreshConsumeTheOldChallenge()
    {
        var renderer = new Renderer(); var service = new MemoryCaptchaService(new Clock(), renderer);
        var first = service.Issue(); var firstCode = renderer.Code;
        Assert.False(service.Verify(first.Id, "000000")); Assert.False(service.Verify(first.Id, firstCode));
        first = service.Issue(); firstCode = renderer.Code; var next = service.Issue(first.Id);
        Assert.False(service.Verify(first.Id, firstCode)); Assert.True(service.Verify(next.Id, renderer.Code));
        Assert.False(service.Verify(null, "ABCDEF")); Assert.False(service.Verify(Guid.NewGuid(), "ABCDEF"));
    }
    [Fact]
    public void ExpiryIsEnforcedAtItsExactBoundaryAndAfterRestart()
    {
        var clock = new Clock(); var renderer = new Renderer(); var service = new MemoryCaptchaService(clock, renderer);
        var issued = service.Issue(); clock.Now = issued.ExpiresAt;
        Assert.False(service.Verify(issued.Id, renderer.Code));
        issued = service.Issue(); Assert.False(new MemoryCaptchaService(clock, renderer).Verify(issued.Id, renderer.Code));
    }
    [Fact]
    public async Task ParallelVerificationCanSucceedOnlyOnce()
    {
        var renderer = new Renderer(); var service = new MemoryCaptchaService(new Clock(), renderer);
        var issued = service.Issue(); var code = renderer.Code;
        var results = await Task.WhenAll(Enumerable.Range(0, 64).Select(_ => Task.Run(() => service.Verify(issued.Id, code))));
        Assert.Single(results, x => x);
    }
    [Fact]
    public void CapacityIsBoundedAndExpiredSlotsAreRecovered()
    {
        var clock = new Clock(); var service = new MemoryCaptchaService(clock, new Renderer());
        for (var i = 0; i < MemoryCaptchaService.Capacity; i++) service.Issue();
        Assert.Throws<CaptchaCapacityException>(() => service.Issue()); clock.Now += MemoryCaptchaService.Lifetime;
        Assert.NotNull(service.Issue());
    }
    [Fact]
    public void RendererCreatesBoundedRgbPngWithNoTextChunks()
    {
        var png = new PngCaptchaRenderer().Render("2A7K9Z");
        Assert.Equal(new byte[] { 137,80,78,71,13,10,26,10 }, png[..8]);
        Assert.Equal(200, BinaryPrimitives.ReadInt32BigEndian(png.AsSpan(16))); Assert.Equal(64, BinaryPrimitives.ReadInt32BigEndian(png.AsSpan(20)));
        using var compressed = new MemoryStream(); var chunks = new List<string>();
        for (var offset = 8; offset < png.Length;)
        {
            var length = BinaryPrimitives.ReadInt32BigEndian(png.AsSpan(offset)); var type = System.Text.Encoding.ASCII.GetString(png, offset + 4, 4);
            chunks.Add(type); if (type == "IDAT") compressed.Write(png, offset + 8, length); offset += 12 + length;
        }
        Assert.Equal(new[] { "IHDR", "IDAT", "IEND" }, chunks);
        compressed.Position = 0; using var z = new ZLibStream(compressed, CompressionMode.Decompress); using var raw = new MemoryStream(); z.CopyTo(raw);
        Assert.Equal(64 * 601, raw.Length); Assert.True(png.Length < 20000);
    }
}
