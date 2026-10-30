using System.Text;
using CommentsService.Infrastructure.TextFiles;
using Xunit;
namespace CommentsService.Data.Tests;

public sealed class TextFileProcessingTests
{
    [Theory]
    [InlineData("Hello\r\nWorld\t!")]
    [InlineData("Привіт / Привет / مرحبا / 😀")]
    [InlineData("<script>window.__txtInjected=true</script>")]
    [InlineData("\uFEFFUTF-8 BOM")]
    public async Task ValidUtf8BytesRemainExactAndNamesAreSanitized(string text)
    {
        var bytes=Encoding.UTF8.GetBytes(text);
        var result=await new Utf8TextFileProcessor().ProcessAsync(new("../../note.TXT",bytes.Length,new MemoryStream(bytes)));
        Assert.True(result.IsValid); Assert.Equal(bytes,result.Value!.Data); Assert.Equal("note.txt",result.Value.FileName);
    }
    [Theory]
    [InlineData(new byte[]{0xff,0xfe,0x41,0})]
    [InlineData(new byte[]{0xc0,0xaf})]
    [InlineData(new byte[]{0xed,0xa0,0x80})]
    [InlineData(new byte[]{0xd0})]
    [InlineData(new byte[]{0,65})]
    [InlineData(new byte[]{27,65})]
    [InlineData(new byte[]{127,65})]
    public async Task InvalidUnicodeOrBinaryControlDataIsRejected(byte[] bytes) =>
        Assert.False((await new Utf8TextFileProcessor().ProcessAsync(new("bad.txt",bytes.Length,new MemoryStream(bytes)))).IsValid);
    [Theory]
    [InlineData("notes.exe")] [InlineData("notes.svg")] [InlineData("notes.txt.exe")]
    public async Task NonTxtExtensionsAreRejected(string name) =>
        Assert.False((await new Utf8TextFileProcessor().ProcessAsync(new(name,1,new MemoryStream([65])))).IsValid);
    [Fact]
    public async Task LimitsUseBytesAndCancellationPropagates()
    {
        var processor=new Utf8TextFileProcessor();var max=Encoding.UTF8.GetBytes(string.Concat(Enumerable.Repeat("😀",25600)));
        var accepted=await processor.ProcessAsync(new("max.txt",max.Length,new MemoryStream(max)));
        Assert.True(accepted.IsValid);Assert.Equal(102400,accepted.Value!.Data.Length);
        Assert.False((await processor.ProcessAsync(new("empty.txt",0,new MemoryStream()))).IsValid);
        Assert.False((await processor.ProcessAsync(new("large.txt",102401,new MemoryStream()))).IsValid);
        Assert.False((await processor.ProcessAsync(new("length.txt",10,new MemoryStream([65])))).IsValid);
        Assert.False((await processor.ProcessAsync(new("overflow.txt",1,new MemoryStream(new byte[102401])))).IsValid);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(()=>processor.ProcessAsync(new("cancel.txt",1,new MemoryStream([65])),new(true)));
    }
    [Fact]
    public async Task LongUnicodeFilenameRemainsValidUtf8()
    {
        var name="A"+string.Concat(Enumerable.Repeat("😀",70))+".txt";
        var result=await new Utf8TextFileProcessor().ProcessAsync(new(name,1,new MemoryStream([65])));
        Assert.True(result.IsValid);Assert.InRange(result.Value!.FileName.Length,1,128);
        Assert.NotEmpty(new UTF8Encoding(false,true).GetBytes(result.Value.FileName));
    }
}
