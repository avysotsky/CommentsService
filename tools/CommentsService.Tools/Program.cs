using CommentsService.Tools;

try
{
    var options = new Options(args);
    switch (options.Command)
    {
        case "init-env": Secrets.Init(options, "main"); break;
        case "init-broker-env": Secrets.Init(options, "broker"); break;
        case "init-search-env": Secrets.Init(options, "search"); break;
        case "init-cloud-env": Secrets.Init(options, "cloud"); break;
        case "migrate": case "check": await DatabaseTools.MigrateOrCheck(options); break;
        case "checkpoint": await DatabaseTools.Checkpoint(options); break;
        case "verify-checkpoint": await DatabaseTools.VerifyCheckpoint(options); break;
        case "verify-schema": await SchemaTools.Verify(options); break;
        case "configure-search-user": await ProviderTools.ConfigureSearch(options); break;
        case "verify-cloud-demo": await ProviderTools.VerifyCloud(options); break;
        case "test-backend": await DatabaseTools.Tests(options); break;
        case "help": Console.WriteLine("Commands: init-env, init-broker-env, init-search-env, init-cloud-env, migrate, check, checkpoint, verify-checkpoint, verify-schema, configure-search-user, verify-cloud-demo, test-backend. Options: --env-file .env; --target development|test|benchmark; --output path; --include-benchmark; --upgrade; --port 8080; --test-db-port 55434. See docs/DOCKER_TOOLS.md."); break;
        default: throw new ToolError("Unknown command; use help.");
    }
    return 0;
}
catch (ToolError error) { Console.Error.WriteLine(error.Message); return 1; }
catch (Exception) { Console.Error.WriteLine("Operation failed. Check selected services, private environment and documented arguments. Provider details and secrets were not printed."); return 1; }
