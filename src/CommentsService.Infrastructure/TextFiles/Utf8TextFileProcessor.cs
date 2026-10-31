using System.Text;
using CommentsService.Application.Common;
using CommentsService.Application.TextFiles;
using CommentsService.Domain.Entities;
namespace CommentsService.Infrastructure.TextFiles;

public sealed class Utf8TextFileProcessor(ApplicationLimits? settings = null) : ITextFileProcessor
{
    private readonly ApplicationLimits limits = settings ?? new();
    private static readonly UTF8Encoding StrictUtf8 = new(false, true);
    public async Task<Validated<PreparedTextFile>> ProcessAsync(TextFileUpload upload, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (upload.Length < 1 || upload.Length > limits.TextFileMaxBytes) return Invalid($"TXT must contain 1..{limits.TextFileMaxBytes} bytes.");
        var name = Path.GetFileName(upload.FileName.Replace('\\', '/'));
        if (!Path.GetExtension(name).Equals(".txt", StringComparison.OrdinalIgnoreCase)) return Invalid("Only TXT files are allowed.");
        using var content = new MemoryStream(); var buffer = new byte[8192]; int count;
        while ((count = await upload.Content.ReadAsync(buffer, cancellationToken)) != 0)
        {
            if (content.Length + count > limits.TextFileMaxBytes) return Invalid($"TXT exceeds {limits.TextFileMaxBytes} bytes.");
            await content.WriteAsync(buffer.AsMemory(0, count), cancellationToken);
        }
        if (content.Length != upload.Length) return Invalid("TXT length is invalid.");
        var bytes = content.ToArray(); string text;
        try { text = StrictUtf8.GetString(bytes); }
        catch (DecoderFallbackException) { return Invalid("TXT must use valid UTF-8 encoding."); }
        if (text.Any(ch => char.IsControl(ch) && ch is not ('\t' or '\r' or '\n'))) return Invalid("TXT cannot contain binary control characters.");
        var safe = new string(Path.GetFileNameWithoutExtension(name).Where(ch => !char.IsControl(ch)).Take(120).ToArray());
        if (safe.Length > 0 && char.IsHighSurrogate(safe[^1])) safe = safe[..^1];
        if (string.IsNullOrWhiteSpace(safe)) safe = "text";
        return Validated<PreparedTextFile>.Success(new(safe + ".txt", bytes));
    }
    private static Validated<PreparedTextFile> Invalid(string message) => Validated<PreparedTextFile>.Invalid(new() { ["textFile"] = [message] });
}
