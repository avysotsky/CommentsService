namespace CommentsService.Infrastructure.Captcha;

public sealed class CaptchaSettings
{
    public int LifetimeSeconds { get; set; } = 300;
    public int Capacity { get; set; } = 2048;
    public int CodeLength { get; set; } = 6;
    public string Alphabet { get; set; } = MemoryCaptchaService.Alphabet;

    public void Validate()
    {
        if (LifetimeSeconds is < 10 or > 3600 || Capacity is < 1 or > 100000 || CodeLength is < 4 or > 10 ||
            Alphabet is null || Alphabet.Length < 8 || Alphabet.Distinct().Count() != Alphabet.Length ||
            Alphabet.Any(character => !MemoryCaptchaService.Alphabet.Contains(character)))
            throw new InvalidOperationException("Invalid CAPTCHA settings.");
    }
}
