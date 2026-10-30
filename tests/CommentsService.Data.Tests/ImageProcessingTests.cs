using CommentsService.Application.Images;
using CommentsService.Infrastructure.Images;
using SkiaSharp;
using Xunit;
namespace CommentsService.Data.Tests;

public sealed class ImageProcessingTests
{
    [Fact]
    public async Task LongUnicodeFilenameDoesNotSplitAnEmoji()
    {
        var bytes=await File.ReadAllBytesAsync(Path.Combine(AppContext.BaseDirectory,"Fixtures","tall.png"));
        var name="A"+string.Concat(Enumerable.Repeat("😀",70))+".png";
        var result=await new SkiaImageProcessor().ProcessAsync(new(name,bytes.Length,new MemoryStream(bytes)));
        Assert.True(result.IsValid); Assert.InRange(result.Value!.FileName.Length,1,128);
        Assert.NotEmpty(new System.Text.UTF8Encoding(false,true).GetBytes(result.Value.FileName));
    }
    [Theory]
    [InlineData("wide.jpg",320,160)] [InlineData("tall.png",30,240)]
    [InlineData("small.gif",16,8)] [InlineData("animated.gif",40,20)] [InlineData("oriented.jpg",120,240)]
    public async Task FormatsResizeWithoutUpscalingAndHonorExif(string file, int width, int height)
    {
        var bytes = await File.ReadAllBytesAsync(Path.Combine(AppContext.BaseDirectory,"Fixtures",file));
        var result = await new SkiaImageProcessor().ProcessAsync(new("../../"+file, bytes.Length, new MemoryStream(bytes)));
        Assert.True(result.IsValid); Assert.Equal(width,result.Value!.Width); Assert.Equal(height,result.Value.Height);
        Assert.EndsWith(".png",result.Value.FileName); Assert.DoesNotContain('/',result.Value.FileName);
        using var codec = SKCodec.Create(new MemoryStream(result.Value.Data)); Assert.Equal(SKEncodedImageFormat.Png,codec.EncodedFormat);
        Assert.Equal(width,codec.Info.Width); Assert.Equal(height,codec.Info.Height);
        Assert.DoesNotContain("STRIP_THIS_METADATA",System.Text.Encoding.UTF8.GetString(result.Value.Data));
        if (file=="animated.gif")
        {
            using var bitmap=SKBitmap.Decode(result.Value.Data); var color=bitmap.GetPixel(10,10); Assert.True(color.Red>200 && color.Blue<30);
        }
        if (file=="tall.png") { using var bitmap=SKBitmap.Decode(result.Value.Data); Assert.InRange(bitmap.GetPixel(10,10).Alpha,110,130); }
    }
    [Theory]
    [InlineData("evil.svg")] [InlineData("evil.txt")] [InlineData("broken.png")] [InlineData("broken.jpg")]
    public async Task InvalidOrUnsupportedFilesAreRejected(string name)
    {
        var bytes=System.Text.Encoding.UTF8.GetBytes("<svg onload='alert(1)'/>");
        Assert.False((await new SkiaImageProcessor().ProcessAsync(new(name,bytes.Length,new MemoryStream(bytes)))).IsValid);
    }
    [Fact]
    public async Task ExtensionSpoofAndTruncatedDataAreRejected()
    {
        var bytes=await File.ReadAllBytesAsync(Path.Combine(AppContext.BaseDirectory,"Fixtures","tall.png"));
        var processor=new SkiaImageProcessor();
        Assert.False((await processor.ProcessAsync(new("fake.jpg",bytes.Length,new MemoryStream(bytes)))).IsValid);
        Assert.False((await processor.ProcessAsync(new("tall.png",bytes.Length,new MemoryStream(bytes[..20])))).IsValid);
        Assert.False((await processor.ProcessAsync(new("empty.png",0,new MemoryStream()))).IsValid);
        Assert.False((await processor.ProcessAsync(new("large.png",5242881,new MemoryStream()))).IsValid);
    }
    [Fact]
    public async Task DimensionLimitsAreCheckedAndCancellationPropagates()
    {
        using var bitmap=new SKBitmap(10001,1); using var image=SKImage.FromBitmap(bitmap); using var png=image.Encode(SKEncodedImageFormat.Png,100);
        var bytes=png.ToArray(); var processor=new SkiaImageProcessor();
        Assert.False((await processor.ProcessAsync(new("wide.png",bytes.Length,new MemoryStream(bytes)))).IsValid);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(()=>processor.ProcessAsync(new("cancel.png",bytes.Length,new MemoryStream(bytes)),new(true)));
    }
}
