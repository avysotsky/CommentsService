using CommentsService.Domain.Entities;

namespace CommentsService.Application.Common;

// Plain application policy; configuration binding belongs to the host, never the domain.
public sealed class ApplicationLimits
{
    public int RootPageSize { get; set; } = 25;
    public int DefaultListLimit { get; set; } = 25;
    public int MaximumListLimit { get; set; } = 100;
    public int UserNameMaxLength { get; set; } = Author.UserNameMaxLength;
    public int EmailMaxLength { get; set; } = Author.EmailMaxLength;
    public int HomePageMaxLength { get; set; } = Author.HomePageMaxLength;
    public int TextMaxLength { get; set; } = Comment.TextMaxLength;
    public int UserAgentMaxLength { get; set; } = Comment.UserAgentMaxLength;
    public int MarkupMaxDepth { get; set; } = 32;
    public int MarkupMaxNodes { get; set; } = 4096;
    public int ImageUploadMaxBytes { get; set; } = 5 * 1024 * 1024;
    public int ImageMaxWidth { get; set; } = 320;
    public int ImageMaxHeight { get; set; } = 240;
    public int ImageDecodeMaxDimension { get; set; } = 10000;
    public int ImageDecodeMaxPixels { get; set; } = 16000000;
    public int ImageProcessingConcurrency { get; set; } = 2;
    public int TextFileMaxBytes { get; set; } = TextAttachment.MaxBytes;

    public void Validate()
    {
        static bool Outside(int value, int maximum) => value < 1 || value > maximum;
        if (Outside(RootPageSize, 100) || Outside(MaximumListLimit, 1000) ||
            Outside(DefaultListLimit, MaximumListLimit) ||
            Outside(UserNameMaxLength, Author.UserNameMaxLength) || Outside(EmailMaxLength, Author.EmailMaxLength) ||
            Outside(HomePageMaxLength, Author.HomePageMaxLength) || Outside(TextMaxLength, Comment.TextMaxLength) ||
            Outside(UserAgentMaxLength, Comment.UserAgentMaxLength) || Outside(MarkupMaxDepth, 32) ||
            Outside(MarkupMaxNodes, 4096) || Outside(ImageUploadMaxBytes, 5 * 1024 * 1024) ||
            Outside(ImageMaxWidth, 320) || Outside(ImageMaxHeight, 240) || Outside(ImageDecodeMaxDimension, 10000) ||
            Outside(ImageDecodeMaxPixels, 16000000) || Outside(ImageProcessingConcurrency, 8) ||
            Outside(TextFileMaxBytes, TextAttachment.MaxBytes))
            throw new InvalidOperationException("Invalid application limits; storage and safe processing ceilings cannot be exceeded.");
    }
}
