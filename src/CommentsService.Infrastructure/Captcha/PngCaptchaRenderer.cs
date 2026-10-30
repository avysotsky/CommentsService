using System.Buffers.Binary;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
namespace CommentsService.Infrastructure.Captcha;

// Raster PNG only: no fonts, platform graphics, SVG text or answer-bearing metadata.
public sealed class PngCaptchaRenderer : ICaptchaImageRenderer
{
    private static readonly string[] Glyphs = [
        "01110/10001/00001/00010/00100/01000/11111", "11110/00001/00001/01110/00001/00001/11110",
        "00010/00110/01010/10010/11111/00010/00010", "11111/10000/10000/11110/00001/00001/11110",
        "01110/10000/10000/11110/10001/10001/01110", "11111/00001/00010/00100/01000/01000/01000",
        "01110/10001/10001/01110/10001/10001/01110", "01110/10001/10001/01111/00001/00001/01110",
        "01110/10001/10001/11111/10001/10001/10001", "11110/10001/10001/11110/10001/10001/11110",
        "01111/10000/10000/10000/10000/10000/01111", "11110/10001/10001/10001/10001/10001/11110",
        "11111/10000/10000/11110/10000/10000/11111", "11111/10000/10000/11110/10000/10000/10000",
        "01111/10000/10000/10111/10001/10001/01111", "10001/10001/10001/11111/10001/10001/10001",
        "00111/00010/00010/00010/10010/10010/01100", "10001/10010/10100/11000/10100/10010/10001",
        "10000/10000/10000/10000/10000/10000/11111", "10001/11011/10101/10101/10001/10001/10001",
        "10001/11001/10101/10011/10001/10001/10001", "11110/10001/10001/11110/10000/10000/10000",
        "01110/10001/10001/10001/10101/10010/01101", "11110/10001/10001/11110/10100/10010/10001",
        "01111/10000/10000/01110/00001/00001/11110", "11111/00100/00100/00100/00100/00100/00100",
        "10001/10001/10001/10001/10001/10001/01110", "10001/10001/10001/10001/10001/01010/00100",
        "10001/10001/10001/10101/10101/11011/10001", "10001/10001/01010/00100/01010/10001/10001",
        "10001/10001/01010/00100/00100/00100/00100", "11111/00001/00010/00100/01000/10000/11111"
    ];
    public byte[] Render(string code)
    {
        const int width = 200, height = 64;
        var pixels = new byte[height * (1 + width * 3)];
        void Dot(int x, int y, byte r, byte g, byte b)
        {
            if (x < 0 || x >= width || y < 0 || y >= height) return;
            var offset = y * (width * 3 + 1) + 1 + x * 3;
            pixels[offset] = r; pixels[offset + 1] = g; pixels[offset + 2] = b;
        }
        for (var y = 0; y < height; y++) for (var x = 0; x < width; x++) Dot(x, y, 247, 248, 241);
        for (var i = 0; i < 600; i++) Dot(RandomNumberGenerator.GetInt32(width), RandomNumberGenerator.GetInt32(height), 165, 180, 170);
        for (var i = 0; i < code.Length; i++)
        {
            var rows = Glyphs[MemoryCaptchaService.Alphabet.IndexOf(code[i])].Split('/');
            var x0 = 9 + i * 30 + RandomNumberGenerator.GetInt32(-2, 3);
            var y0 = 14 + RandomNumberGenerator.GetInt32(-3, 4);
            for (var y = 0; y < 7; y++) for (var x = 0; x < 5; x++) if (rows[y][x] == '1')
                for (var dy = 0; dy < 5; dy++) for (var dx = 0; dx < 5; dx++) Dot(x0 + x * 5 + dx, y0 + y * 5 + dy, 30, 65, 50);
        }
        using var png = new MemoryStream(); png.Write(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 });
        void Chunk(string type, byte[] data)
        {
            var name = Encoding.ASCII.GetBytes(type); var length = new byte[4];
            BinaryPrimitives.WriteInt32BigEndian(length, data.Length); png.Write(length); png.Write(name); png.Write(data);
            uint crc = 0xffffffff;
            foreach (var value in name.Concat(data))
            {
                crc ^= value;
                for (var bit = 0; bit < 8; bit++) crc = (crc >> 1) ^ ((crc & 1) != 0 ? 0xedb88320u : 0u);
            }
            var checksum = new byte[4]; BinaryPrimitives.WriteUInt32BigEndian(checksum, crc ^ 0xffffffff); png.Write(checksum);
        }
        var header = new byte[13]; BinaryPrimitives.WriteInt32BigEndian(header, width); BinaryPrimitives.WriteInt32BigEndian(header.AsSpan(4), height);
        header[8] = 8; header[9] = 2; Chunk("IHDR", header);
        using var compressed = new MemoryStream();
        using (var zlib = new ZLibStream(compressed, CompressionLevel.Fastest, true)) zlib.Write(pixels);
        Chunk("IDAT", compressed.ToArray()); Chunk("IEND", []); return png.ToArray();
    }
}
