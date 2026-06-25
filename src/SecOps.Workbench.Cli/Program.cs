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
        if (args.Length != 2)
        {
            Console.Error.WriteLine("Usage: secops-workbench triage <alert.json>");
            return 2;
        }

        try
        {
            var json = await File.ReadAllTextAsync(args[1]);
            var alert = AlertParser.Parse(json);
            var result = new TriageEngine().Triage(alert);
            Console.WriteLine(result.ToMarkdown());
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
                            secops-workbench triage <alert.json>

                          This is an engineering workbench for synthetic alert triage and safe playbook recommendations.
                          """);
    }
}
