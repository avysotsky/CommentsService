namespace CommentsService.Domain.Entities;

public sealed class TextAttachment
{
    public const int MaxBytes = 100 * 1024;
    private byte[] _data = [];
    private TextAttachment() { }
    public TextAttachment(Comment comment, string fileName, byte[] data)
    {
        ArgumentNullException.ThrowIfNull(comment); ArgumentException.ThrowIfNullOrWhiteSpace(fileName); ArgumentNullException.ThrowIfNull(data);
        if (fileName.Length > 128 || fileName.Any(char.IsControl)) throw new ArgumentException("Invalid filename.", nameof(fileName));
        if (data.Length is < 1 or > MaxBytes) throw new ArgumentException("Text attachment must contain 1..102400 bytes.", nameof(data));
        FileName = fileName; _data = data.ToArray(); SizeBytes = data.Length;
        Comment = comment; CommentId = comment.Id; comment.AttachTextFile(this);
    }
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid CommentId { get; private set; }
    public Comment Comment { get; private set; } = null!;
    public string FileName { get; private set; } = null!;
    public int SizeBytes { get; private set; }
    public byte[] Data { get => _data.ToArray(); private set => _data = value; }
}
