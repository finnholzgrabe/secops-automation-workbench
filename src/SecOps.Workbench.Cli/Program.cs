using SecOps.Workbench.Cli;
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

        if (args[0] == "playbooks")
        {
            return RunPlaybooks(args);
        }

        if (args[0] == "detections")
        {
            return RunDetections(args);
        }

        if (args[0] == "simulate")
        {
            return await RunSimulateAsync(args);
        }

        if (args[0] == "batch")
        {
            return await RunBatchAsync(args);
        }

        Console.Error.WriteLine($"Unknown command '{args[0]}'.");
        PrintHelp();
        return 2;
    }

    private static async Task<int> RunTriageAsync(string[] args)
    {
        string? alertPath = null;
        var format = ReportFormat.Markdown;
        var formatSpecified = false;
        var caseNote = false;
        string? outPath = null;
        string? attackLayerPath = null;

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

                    formatSpecified = true;
                    break;

                case "--case-note":
                    caseNote = true;
                    break;

                case "--out":
                    if (i + 1 >= args.Length)
                    {
                        Console.Error.WriteLine("Missing value for --out (expected a file path).");
                        return 2;
                    }

                    outPath = args[++i];
                    break;

                case "--attack-layer":
                    if (i + 1 >= args.Length)
                    {
                        Console.Error.WriteLine("Missing value for --attack-layer (expected a file path).");
                        return 2;
                    }

                    attackLayerPath = args[++i];
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
            Console.Error.WriteLine("Usage: secops-workbench triage <alert.json> [--format markdown|json] [--case-note] [--out <path>]");
            return 2;
        }

        if (caseNote && formatSpecified && format != ReportFormat.Markdown)
        {
            Console.Error.WriteLine("Case notes are only available in markdown; drop --format or --case-note.");
            return 2;
        }

        try
        {
            var json = await File.ReadAllTextAsync(alertPath);
            var alert = AlertParser.Parse(json);
            var playbooks = PlaybookStore.LoadForTriage(PlaybookStore.DefaultDirectory);
            var result = new TriageEngine(playbooks).Triage(alert);
            var report = caseNote ? CaseNote.Render(result) : result.Render(format);

            if (outPath is null)
            {
                Console.WriteLine(report);
            }
            else
            {
                await File.WriteAllTextAsync(outPath, report);
                Console.Error.WriteLine($"Report written to {outPath}");
            }

            if (attackLayerPath is not null)
            {
                var techniques = TechniqueFrequency.Tally(new[] { result.TechniqueIds });
                var layer = AttackNavigatorLayer.Build(
                    $"Triage {alert.Id}",
                    $"ATT&CK techniques mapped for alert {alert.Id} by the SecOps Automation Workbench (synthetic).",
                    techniques);
                await File.WriteAllTextAsync(attackLayerPath, layer);
                Console.Error.WriteLine($"ATT&CK Navigator layer written to {attackLayerPath}");
            }

            return 0;
        }
        catch (Exception ex) when (ex is IOException or InvalidDataException or System.Text.Json.JsonException or ArgumentException)
        {
            Console.Error.WriteLine($"Triage failed: {ex.Message}");
            return 1;
        }
    }

    private static int RunPlaybooks(string[] args)
    {
        var subcommand = args.Length >= 2 ? args[1] : null;
        var directory = args.Length >= 3 ? args[2] : PlaybookStore.DefaultDirectory;

        if (subcommand is not ("list" or "validate"))
        {
            Console.Error.WriteLine("Usage: secops-workbench playbooks <list|validate> [directory]");
            return 2;
        }

        if (!Directory.Exists(directory))
        {
            Console.Error.WriteLine($"Playbook directory '{directory}' was not found.");
            return 1;
        }

        var loaded = PlaybookStore.LoadDirectory(directory);
        if (loaded.Count == 0)
        {
            Console.Error.WriteLine($"No playbook files (*.json) found in '{directory}'.");
            return 1;
        }

        return subcommand == "list" ? ListPlaybooks(loaded) : ValidatePlaybooks(loaded);
    }

    private static int ListPlaybooks(IReadOnlyList<PlaybookStore.LoadedPlaybook> loaded)
    {
        foreach (var item in loaded)
        {
            if (item is { Playbook: { } playbook, Errors.Count: 0 })
            {
                var techniques = playbook.Techniques.Count == 0 ? "-" : string.Join(", ", playbook.Techniques);
                Console.WriteLine($"{playbook.Id}  [{playbook.Category}]  techniques: {techniques}");
                Console.WriteLine($"    {playbook.Title}");
            }
            else
            {
                Console.WriteLine($"{Path.GetFileName(item.Path)}  (invalid - run 'playbooks validate')");
            }
        }

        return 0;
    }

    private static int ValidatePlaybooks(IReadOnlyList<PlaybookStore.LoadedPlaybook> loaded)
    {
        var invalid = 0;

        foreach (var item in loaded)
        {
            var name = Path.GetFileName(item.Path);
            if (item.Errors.Count == 0)
            {
                Console.WriteLine($"OK    {name}");
            }
            else
            {
                invalid++;
                Console.WriteLine($"FAIL  {name}");
                foreach (var error in item.Errors)
                {
                    Console.WriteLine($"        - {error}");
                }
            }
        }

        Console.WriteLine();
        Console.WriteLine($"{loaded.Count - invalid}/{loaded.Count} playbooks valid.");
        return invalid == 0 ? 0 : 1;
    }

    private static async Task<int> RunBatchAsync(string[] args)
    {
        string? directory = null;
        var format = "json";
        string? outPath = null;

        for (var i = 1; i < args.Length; i++)
        {
            var arg = args[i];
            switch (arg)
            {
                case "--format":
                    if (i + 1 >= args.Length || args[i + 1] is not ("json" or "csv"))
                    {
                        Console.Error.WriteLine("Invalid or missing value for --format. Use 'json' or 'csv'.");
                        return 2;
                    }

                    format = args[++i];
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

                    if (directory is not null)
                    {
                        Console.Error.WriteLine("Only one alert directory can be batched at a time.");
                        return 2;
                    }

                    directory = arg;
                    break;
            }
        }

        if (directory is null)
        {
            Console.Error.WriteLine("Usage: secops-workbench batch <alert-directory> [--format json|csv] [--out <path>]");
            return 2;
        }

        if (!Directory.Exists(directory))
        {
            Console.Error.WriteLine($"Alert directory '{directory}' was not found.");
            return 1;
        }

        var files = Directory.EnumerateFiles(directory, "*.json")
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToList();

        if (files.Count == 0)
        {
            Console.Error.WriteLine($"No alert files (*.json) found in '{directory}'.");
            return 1;
        }

        var engine = new TriageEngine(PlaybookStore.LoadForTriage(PlaybookStore.DefaultDirectory));
        var results = new List<TriageResult>();

        foreach (var file in files)
        {
            try
            {
                results.Add(engine.Triage(AlertParser.Parse(await File.ReadAllTextAsync(file))));
            }
            catch (Exception ex) when (ex is IOException or InvalidDataException or System.Text.Json.JsonException or ArgumentException)
            {
                Console.Error.WriteLine($"Skipping {Path.GetFileName(file)}: {ex.Message}");
            }
        }

        if (results.Count == 0)
        {
            Console.Error.WriteLine("No valid alerts were triaged.");
            return 1;
        }

        var summary = TriageSummary.From(results);
        var output = format == "csv" ? summary.ToCsv() : summary.ToJson();

        if (outPath is null)
        {
            Console.WriteLine(output);
        }
        else
        {
            await File.WriteAllTextAsync(outPath, output);
            Console.Error.WriteLine($"Batch summary written to {outPath}");
        }

        return 0;
    }

    private static async Task<int> RunSimulateAsync(string[] args)
    {
        string? scenarioPath = null;
        var format = ReportFormat.Markdown;
        string? outPath = null;
        string? attackLayerPath = null;

        for (var i = 1; i < args.Length; i++)
        {
            var arg = args[i];
            switch (arg)
            {
                case "--format":
                    if (i + 1 >= args.Length || !ReportFormats.TryParse(args[++i], out format) || format == ReportFormat.Html)
                    {
                        Console.Error.WriteLine("Invalid or missing value for --format. Use 'markdown' or 'json'.");
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

                case "--attack-layer":
                    if (i + 1 >= args.Length)
                    {
                        Console.Error.WriteLine("Missing value for --attack-layer (expected a file path).");
                        return 2;
                    }

                    attackLayerPath = args[++i];
                    break;

                default:
                    if (arg.StartsWith('-'))
                    {
                        Console.Error.WriteLine($"Unknown option '{arg}'.");
                        return 2;
                    }

                    if (scenarioPath is not null)
                    {
                        Console.Error.WriteLine("Only one scenario file can be simulated at a time.");
                        return 2;
                    }

                    scenarioPath = arg;
                    break;
            }
        }

        if (scenarioPath is null)
        {
            Console.Error.WriteLine("Usage: secops-workbench simulate <scenario.json> [--format markdown|json] [--out <path>] [--attack-layer <path>]");
            return 2;
        }

        try
        {
            var json = await File.ReadAllTextAsync(scenarioPath);
            var scenario = ScenarioParser.Parse(json);
            var playbooks = PlaybookStore.LoadForTriage(PlaybookStore.DefaultDirectory);
            var report = new ScenarioRunner(new TriageEngine(playbooks)).Run(scenario);
            var output = report.Render(format);

            if (outPath is null)
            {
                Console.WriteLine(output);
            }
            else
            {
                await File.WriteAllTextAsync(outPath, output);
                Console.Error.WriteLine($"Incident report written to {outPath}");
            }

            if (attackLayerPath is not null)
            {
                var layer = AttackNavigatorLayer.Build(
                    scenario.Name,
                    $"ATT&CK techniques observed across scenario '{scenario.Name}' (synthetic).",
                    report.Techniques);
                await File.WriteAllTextAsync(attackLayerPath, layer);
                Console.Error.WriteLine($"ATT&CK Navigator layer written to {attackLayerPath}");
            }

            return 0;
        }
        catch (Exception ex) when (ex is IOException or InvalidDataException or System.Text.Json.JsonException or ArgumentException)
        {
            Console.Error.WriteLine($"Simulation failed: {ex.Message}");
            return 1;
        }
    }

    private static int RunDetections(string[] args)
    {
        var subcommand = args.Length >= 2 ? args[1] : null;
        var directory = args.Length >= 3 ? args[2] : DetectionStore.DefaultDirectory;

        if (subcommand != "lint")
        {
            Console.Error.WriteLine("Usage: secops-workbench detections lint [directory]");
            return 2;
        }

        if (!Directory.Exists(directory))
        {
            Console.Error.WriteLine($"Detections directory '{directory}' was not found.");
            return 1;
        }

        var results = DetectionStore.LintDirectory(directory);
        if (results.Count == 0)
        {
            Console.Error.WriteLine($"No detection files (*.yml, *.yaml) found in '{directory}'.");
            return 1;
        }

        var failed = 0;
        foreach (var detection in results)
        {
            var name = Path.GetFileName(detection.Path);
            if (detection.Issues.Count == 0)
            {
                Console.WriteLine($"OK    {name}");
            }
            else
            {
                failed++;
                Console.WriteLine($"FAIL  {name}");
                foreach (var issue in detection.Issues)
                {
                    Console.WriteLine($"        - {issue}");
                }
            }
        }

        Console.WriteLine();
        Console.WriteLine($"{results.Count - failed}/{results.Count} detections passed linting.");
        return failed == 0 ? 0 : 1;
    }

    private static void PrintHelp()
    {
        Console.WriteLine("""
                          secops-workbench

                          Usage:
                            secops-workbench --help
                            secops-workbench version
                            secops-workbench triage <alert.json> [--format markdown|json|html] [--case-note] [--out <path>]
                            secops-workbench simulate <scenario.json> [--format markdown|json] [--out <path>] [--attack-layer <path>]
                            secops-workbench batch <alert-directory> [--format json|csv] [--out <path>]
                            secops-workbench playbooks <list|validate> [directory]
                            secops-workbench detections lint [directory]

                          Options for 'triage':
                            --format markdown|json|html  Output format (default: markdown).
                            --case-note              Emit an analyst case note (markdown) instead of the report.
                            --out <path>             Write the report to a file instead of stdout.
                            --attack-layer <path>    Also write an ATT&CK Navigator layer JSON file.

                          The 'playbooks' command reads JSON playbooks from a directory (default: playbooks/).
                          The 'detections' command lints Sigma-inspired rules from a directory (default: detections/).

                          This is an engineering workbench for synthetic alert triage and safe playbook recommendations.
                          """);
    }
}
