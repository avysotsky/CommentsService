using System.Security.Cryptography;
using System.Text;
namespace CommentsService.Infrastructure.Search;
public static class SearchIndexName
{
    public static string ForDatabase(string prefix, string database) => prefix + "-" +
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(database))).ToLowerInvariant()[..12];
}
