using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Spectre.Cli;
using XDepend.Core;

namespace ProjectDependenciesScanner
{
    class Program
    {
        static int Main(string[] args)
        {
            var app = new CommandApp<ScanCommand>();

            app.Configure(config =>
            {
                config.SetApplicationName("Project Dependencies Scanner");
                config.AddCommand<ScanCommand>("list")
                    .WithDescription("List project dependencies.")
                    .WithExample(new[] { "list", "ProjectPath", "--References", "--ExportFormat", "JSON" })
                    .WithExample(new[] { "list", "SolutionPath", "--Packages", "--ExportFormat", "CSV" });
            });

            return app.Run(args);
        }
    }

    public class ScanCommand : Command<ScanSettings>
    {
        public override int Execute(CommandContext context, ScanSettings settings)
        {
            if (string.IsNullOrEmpty(settings.Path))
            {
                Console.Error.WriteLine("Path must be specified.");
                return 1;
            }

            if (!File.Exists(settings.Path))
            {
                Console.Error.WriteLine($"Path not found: {settings.Path}");
                return 1;
            }

            if (settings.References && settings.Packages)
            {
                Console.Error.WriteLine("Only one of --References or --Packages can be specified.");
                return 1;
            }

            IEnumerable<string> dependencies = null;

            if (settings.References)
            {
                if(settings.Path.EndsWith("csproj"))
                {
                    dependencies = SolutionHelper.GetProjectReferences(settings.Path);

                }
                else if(settings.Path.EndsWith("sln"))
                {
                    dependencies = SolutionHelper.GetSolutionReferences(settings.Path);
                }
            }
            else if (settings.Packages)
            {
                if (settings.Path.EndsWith("csproj"))
                {
                    dependencies = SolutionHelper.GetPackageReferences(settings.Path);
                }
                else if (settings.Path.EndsWith("sln"))
                {
                    dependencies = SolutionHelper.GetSolutionPackages(settings.Path);
                }
            }
            else
            {
                Console.Error.WriteLine("One of --References or --Packages must be specified.");
                return 1;
            }

            if(dependencies is null)
            {
                return 1;
            }

            switch (settings.ExportFormat)
            {
                case ExportFormat.JSON:
                    if(string.IsNullOrEmpty(settings.ExportPath))
                    {
                        Console.WriteLine(JsonConvert.SerializeObject(dependencies));
                    }
                    else
                    {
                        File.WriteAllText(settings.ExportPath, JsonConvert.SerializeObject(dependencies));
                    }
                    break;

                case ExportFormat.CSV:
                    if (string.IsNullOrEmpty(settings.ExportPath))
                    {
                        Console.WriteLine(string.Join(",", dependencies));
                    }
                    else
                    {
                        File.WriteAllText(settings.ExportPath, string.Join(",", dependencies));
                    }
                    break;

                case ExportFormat.TXT:
                default:
                    if (string.IsNullOrEmpty(settings.ExportPath))
                    {
                        Console.WriteLine(string.Join(Environment.NewLine, dependencies));
                    }
                    else
                    {
                        File.WriteAllText(settings.ExportPath, string.Join(Environment.NewLine, dependencies));
                    }
                    break;
            }

            return 0;
        }
    }

    public class ScanSettings : CommandSettings
    {
        [CommandArgument(0, "<Path>")]
        public string Path { get; set; }

        [CommandOption("--References")]
        public bool References { get; set; }

        [CommandOption("--Packages")]
        public bool Packages { get; set; }

        [CommandOption("--ExportFormat")]
        public ExportFormat ExportFormat { get; set; }

        [CommandOption("--ExportPath")]
        public string ExportPath { get; set; }
    }

    public enum ExportFormat
    {
        JSON,
        TXT,
        CSV
    }
}
