using SecOps.Workbench.Core;

return await Cli.RunAsync(args);

internal static class Cli
{
    public static async Task<int> RunAsync(string[] args)
    {
        if (args.Length == 0 || args[0] is "--help" or "-h" or "help")
        {
            PrintHelp();
            return 0;
        }

        if (args[0] is "--version" or "version")
        {
            Console.WriteLine("secops-workbench 0.1.0");
            return 0;
        }

        if (args[0] == "triage")
        {
            return await RunTriageAsync(args);
        }

        Console.Error.WriteLine($"Unknown command '{args[0]}'.");
        PrintHelp();
        return 2;
    }

    private static async Task<int> RunTriageAsync(string[] args)
    {
        string? alertPath = null;
        var format = ReportFormat.Markdown;
        string? outPath = null;

        for (var i = 1; i < args.Length; i++)
        {
            var arg = args[i];
            switch (arg)
            {
                case "--format":
                    if (i + 1 >= args.Length)
                    {
                        Console.Error.WriteLine("Missing value for --format (expected 'markdown' or 'json').");
                        return 2;
                    }

                    if (!ReportFormats.TryParse(args[++i], out format))
                    {
                        Console.Error.WriteLine($"Unknown format '{args[i]}'. Use 'markdown' or 'json'.");
                        return 2;
                    }

                    break;

                case "--out":
                    if (i + 1 >= args.Length)
                    {
                        Console.Error.WriteLine("Missing value for --out (expected a file path).");
                        return 2;
                    }

                    outPath = args[++i];
                    break;

                default:
                    if (arg.StartsWith('-'))
                    {
                        Console.Error.WriteLine($"Unknown option '{arg}'.");
                        return 2;
                    }

                    if (alertPath is not null)
                    {
                        Console.Error.WriteLine("Only one alert file can be triaged at a time.");
                        return 2;
                    }

                    alertPath = arg;
                    break;
            }
        }

        if (alertPath is null)
        {
            Console.Error.WriteLine("Usage: secops-workbench triage <alert.json> [--format markdown|json] [--out <path>]");
            return 2;
        }

        try
        {
            var json = await File.ReadAllTextAsync(alertPath);
            var alert = AlertParser.Parse(json);
            var result = new TriageEngine().Triage(alert);
            var report = result.Render(format);

            if (outPath is null)
            {
                Console.WriteLine(report);
            }
            else
            {
                await File.WriteAllTextAsync(outPath, report);
                Console.Error.WriteLine($"Report written to {outPath}");
            }

            return 0;
        }
        catch (Exception ex) when (ex is IOException or InvalidDataException or System.Text.Json.JsonException)
        {
            Console.Error.WriteLine($"Triage failed: {ex.Message}");
            return 1;
        }
    }

    private static void PrintHelp()
    {
        Console.WriteLine("""
                          secops-workbench

                          Usage:
                            secops-workbench --help
                            secops-workbench version
                            secops-workbench triage <alert.json> [--format markdown|json] [--out <path>]

                          Options for 'triage':
                            --format markdown|json   Output format (default: markdown).
                            --out <path>             Write the report to a file instead of stdout.

                          This is an engineering workbench for synthetic alert triage and safe playbook recommendations.
                          """);
    }
}
