namespace CommentsService.Api.Configuration;

public static class RuntimeConfiguration
{
    public static RuntimeSettings Load(ConfigurationManager configuration, string[] args)
    {
        var selected = Environment.GetEnvironmentVariable("APP_CONFIG");
        try
        {
            configuration.AddJsonFile(selected is null ? Path.Combine(AppContext.BaseDirectory, "config.json") : Path.GetFullPath(selected),
                optional: selected is null, reloadOnChange: false);
            configuration.AddEnvironmentVariables();
            configuration.AddCommandLine(args);
            var settings = configuration.GetSection("Runtime").Get<RuntimeSettings>() ?? new();
            settings.Validate();
            return settings;
        }
        catch (Exception exception) when (exception is IOException or FormatException or InvalidOperationException or ArgumentException)
        {
            throw new InvalidOperationException("Runtime configuration could not be loaded or validated.");
        }
    }
}
