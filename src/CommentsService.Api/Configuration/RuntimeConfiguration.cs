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
            var postgres = new Dictionary<string, string?>();
            foreach (var pair in new Dictionary<string, string> { ["PGHOST"] = "Host", ["PGPORT"] = "Port",
                ["PGDATABASE"] = "Database", ["PGUSER"] = "Username" })
                if (Environment.GetEnvironmentVariable(pair.Key) is { } value)
                    postgres["Runtime:Database:" + pair.Value] = value;
            configuration.AddInMemoryCollection(postgres);
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
