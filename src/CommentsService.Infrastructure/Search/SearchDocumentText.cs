using System.Text;
using System.Xml;
namespace CommentsService.Infrastructure.Search;
public static class SearchDocumentText
{
    public static string Extract(string text)
    {
        try
        {
            using var reader = XmlReader.Create(new StringReader("<root>" + text + "</root>"), new XmlReaderSettings
            { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null, MaxCharactersInDocument = 65549 });
            var result = new StringBuilder();
            while (reader.Read())
            {
                if (reader.NodeType is XmlNodeType.Text or XmlNodeType.Whitespace or XmlNodeType.SignificantWhitespace) result.Append(reader.Value);
                else if (reader.NodeType == XmlNodeType.EndElement) result.Append(' ');
            }
            return result.ToString().Trim();
        }
        catch (XmlException) { return text; } // Legacy/plain literal '<' and '&' stay literal; never rendered from the index.
    }
}
