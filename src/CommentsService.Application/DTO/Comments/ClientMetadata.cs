using System.Net;
using CommentsService.Domain.Entities;

namespace CommentsService.Application.Comments.CreateComment;

public sealed record ClientMetadata(IPAddress IpAddress, string UserAgent)
{
    public const int UserAgentMaxLength = Comment.UserAgentMaxLength;
}
