using CommentsService.Application.Common;
using CommentsService.Application.Images;
using SkiaSharp;
namespace CommentsService.Infrastructure.Images;

public sealed class SkiaImageProcessor : IImageProcessor
{
    public const int MaxUploadBytes = 5 * 1024 * 1024;
    private readonly SemaphoreSlim slots = new(2);
    public async Task<Validated<PreparedImage>> ProcessAsync(ImageUpload upload, CancellationToken cancellationToken = default)
    {
        if (upload.Length is < 1 or > MaxUploadBytes) return Invalid("Image must be between 1 byte and 5 MiB.");
        var name = Path.GetFileName(upload.FileName.Replace('\\', '/'));
        var extension = Path.GetExtension(name).ToLowerInvariant();
        if (extension is not (".jpg" or ".jpeg" or ".gif" or ".png")) return Invalid("Only JPG, GIF and PNG are allowed.");
        using var source = new MemoryStream();
        var buffer = new byte[81920]; int read;
        while ((read = await upload.Content.ReadAsync(buffer, cancellationToken)) != 0)
        {
            if (source.Length + read > MaxUploadBytes) return Invalid("Image exceeds 5 MiB.");
            await source.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
        }
        if (source.Length != upload.Length) return Invalid("Image length is invalid.");
        await slots.WaitAsync(cancellationToken);
        try
        {
            using var encoded = SKData.CreateCopy(source.ToArray()); using var codec = SKCodec.Create(encoded);
            if (codec is null) return Invalid("Image could not be decoded.");
            var format = codec.EncodedFormat;
            if (!((extension is ".jpg" or ".jpeg" && format == SKEncodedImageFormat.Jpeg) ||
                (extension == ".gif" && format == SKEncodedImageFormat.Gif) || (extension == ".png" && format == SKEncodedImageFormat.Png)))
                return Invalid("Image contents do not match its extension.");
            var w = codec.Info.Width; var h = codec.Info.Height;
            if (w < 1 || h < 1 || w > 10000 || h > 10000 || (long)w * h > 16000000) return Invalid("Image dimensions exceed safe decoding limits.");
            cancellationToken.ThrowIfCancellationRequested();
            using var bitmap = new SKBitmap(w, h);
            if (codec.GetPixels(bitmap.Info, bitmap.GetPixels()) != SKCodecResult.Success) return Invalid("Image is incomplete or corrupt.");
            var origin = (int)codec.EncodedOrigin; var swap = origin >= 5;
            var ow = swap ? h : w; var oh = swap ? w : h;
            var ratio = Math.Min(1d, Math.Min(320d / ow, 240d / oh));
            var tw = Math.Max(1, (int)Math.Floor(ow * ratio)); var th = Math.Max(1, (int)Math.Floor(oh * ratio));
            using var target = new SKBitmap(tw, th); using var canvas = new SKCanvas(target); canvas.Clear(SKColors.Transparent);
            float a = 1, b = 0, c = 0, d = 0, e = 1, f = 0;
            switch (origin)
            {
                case 2: a = -1; c = w; break;
                case 3: a = -1; c = w; e = -1; f = h; break;
                case 4: e = -1; f = h; break;
                case 5: a = 0; b = 1; d = 1; e = 0; break;
                case 6: a = 0; b = -1; c = h; d = 1; e = 0; break;
                case 7: a = 0; b = -1; c = h; d = -1; e = 0; f = w; break;
                case 8: a = 0; b = 1; d = -1; e = 0; f = w; break;
            }
            var sx = (float)tw / ow; var sy = (float)th / oh;
            canvas.SetMatrix(new SKMatrix { ScaleX = a * sx, SkewX = b * sx, TransX = c * sx, SkewY = d * sy, ScaleY = e * sy, TransY = f * sy, Persp2 = 1 });
            using var image = SKImage.FromBitmap(bitmap);
            canvas.DrawImage(image, new SKRect(0, 0, w, h), new SKSamplingOptions(SKFilterMode.Linear)); canvas.Flush();
            using var output = SKImage.FromBitmap(target); using var png = output.Encode(SKEncodedImageFormat.Png, 100);
            var safe = new string(Path.GetFileNameWithoutExtension(name).Where(ch => !char.IsControl(ch)).Take(120).ToArray());
            if (safe.Length > 0 && char.IsHighSurrogate(safe[^1])) safe = safe[..^1];
            if (string.IsNullOrWhiteSpace(safe)) safe = "image";
            cancellationToken.ThrowIfCancellationRequested();
            return Validated<PreparedImage>.Success(new(safe + ".png", png.ToArray(), tw, th));
        }
        finally { slots.Release(); }
    }
    private static Validated<PreparedImage> Invalid(string message) => Validated<PreparedImage>.Invalid(new() { ["image"] = [message] });
}
