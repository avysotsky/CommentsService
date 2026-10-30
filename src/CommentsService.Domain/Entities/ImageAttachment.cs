namespace CommentsService.Domain.Entities;

public sealed class ImageAttachment
{
    public const int MaxBytes = 524288;
    private byte[] _data = [];
    private ImageAttachment() { }
    public ImageAttachment(Comment comment, string fileName, byte[] data, int width, int height)
    {
        ArgumentNullException.ThrowIfNull(comment); ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
        ArgumentNullException.ThrowIfNull(data);
        if (fileName.Length > 128 || fileName.Any(char.IsControl)) throw new ArgumentException("Invalid filename.", nameof(fileName));
        if (data.Length is < 1 or > MaxBytes || width is < 1 or > 320 || height is < 1 or > 240)
            throw new ArgumentException("Invalid image dimensions or size.");
        FileName = fileName; _data = data.ToArray(); Width = width; Height = height;
        Comment = comment; CommentId = comment.Id; comment.AttachImage(this);
    }
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid CommentId { get; private set; }
    public Comment Comment { get; private set; } = null!;
    public string FileName { get; private set; } = null!;
    public int Width { get; private set; }
    public int Height { get; private set; }
    public byte[] Data { get => _data.ToArray(); private set => _data = value; }
}
