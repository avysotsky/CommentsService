using CommentsService.Application.Comments.Markup;
using Xunit;
namespace CommentsService.Application.Tests;

public sealed class XhtmlMarkupTests
{
    private readonly ICommentMarkup markup = new XhtmlCommentMarkup();
    [Theory]
    [InlineData("<strong>Bold <i>italic <code>x &amp; y</code></i></strong>")]
    [InlineData("<a href=\"https://example.test/?a=1&amp;b=2\" title=\"&quot;hello&quot;\">Привіт 😀</a>")]
    [InlineData("1 < 2 && 3 > 1")]
    [InlineData("literal &lt;script&gt;alert(1)&lt;/script&gt;")]
    public void AcceptedTextProducesOnlySafeMarkup(string text)
    {
        var result = markup.Validate(text);
        Assert.True(result.IsValid);
        Assert.DoesNotContain("<script>", result.Value!.Html);
        Assert.DoesNotContain("javascript:", result.Value.Html);
    }
    [Theory]
    [InlineData("<script>alert(1)</script>")]
    [InlineData("<img src=\"x\" onerror=\"alert(1)\" />")]
    [InlineData("<strong onclick=\"alert(1)\">x</strong>")]
    [InlineData("<i><strong>x</i></strong>")]
    [InlineData("<i>x")]
    [InlineData("<STRONG>x</STRONG>")]
    [InlineData("<a>x</a>")]
    [InlineData("<a href=\"javascript:alert(1)\">x</a>")]
    [InlineData("<a href=\"java&#x73;cript:alert(1)\">x</a>")]
    [InlineData("<a href=\"https://user:pass@example.test/\">x</a>")]
    [InlineData("<a href=\"/relative\">x</a>")]
    [InlineData("<a href=\"https://example.test/&#10;path\">x</a>")]
    [InlineData("<a href=\"https://example.test/\"><i><a href=\"https://example.test/\">x</a></i></a>")]
    [InlineData("<i xmlns=\"urn:evil\">x</i>")]
    [InlineData("<!-- comment -->x")]
    [InlineData("<![CDATA[x]]>")]
    [InlineData("<!DOCTYPE root [<!ENTITY x SYSTEM 'file:///etc/passwd'>]>&x;")]
    [InlineData("<?pi x?>text")]
    [InlineData("<code>x & y</code>")]
    [InlineData("<strong> </strong>")]
    [InlineData("text\u0001")]
    public void UnsafeOrMalformedMarkupIsRejected(string text) => Assert.False(markup.Validate(text).IsValid);

    [Fact]
    public void CanonicalOutputEncodesTextAndAttributesAndAddsLinkProtection()
    {
        var html = markup.Validate("<a href=\"https://example.test/\" title=\"&quot;&lt;img&gt;\">&lt;script&gt;</a>").Value!.Html;
        Assert.Contains("rel=\"noopener noreferrer nofollow\"", html);
        Assert.Contains("&quot;&lt;img&gt;", html);
        Assert.DoesNotContain("<script>", html);
        Assert.Equal("1 &lt; 2 &amp;&amp; 3 &gt; 1", markup.Validate("1 < 2 && 3 > 1").Value!.Html);
    }
    [Fact]
    public void ComplexityIsBounded()
    {
        Assert.False(markup.Validate(string.Concat(Enumerable.Repeat("<i>", 33)) + "x" + string.Concat(Enumerable.Repeat("</i>", 33))).IsValid);
        Assert.False(markup.Validate(string.Concat(Enumerable.Repeat("<i>x</i>", 1400))).IsValid);
    }
}
