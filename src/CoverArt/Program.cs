using System;
using System.IO;
using System.Linq;
using ConsoleArt.Lib;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Mono.Options;

namespace CoverArt
{
    internal class Program
    {
        private static int Main(string[] args)
        {
            var appLocation = Path.GetDirectoryName(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName);
            Directory.SetCurrentDirectory(appLocation);
            NLog.LogManager.LogFactory.SetCandidateConfigFilePaths(NLog.LogManager.LogFactory.GetCandidateConfigFilePaths().Concat(new[] {Path.Combine(appLocation, "nlog.config")}));

            Logger.Init();

            var configuration = new ConfigurationBuilder()
                .SetBasePath(appLocation)
                .AddJsonFile("appsettings.json")
                .Build();

            var config = configuration.Get<Config>();

            var showHelp = false;
            var parseOnly = false;
            var listOnly = false;

            var p = new OptionSet
            {
                {"h|hjelp", "Vis denne meldingen", x => showHelp = true},
                {"c|config", "Skriver ut gjeldende innstillinger", x => parseOnly = true},
                {"l|list", "Skriver ut alle relevante filer", x => listOnly = true}
            };

            var unknown = p.Parse(args);

            if (unknown.Count > 0)
            {
                Console.WriteLine($"Unknown arguments: {string.Join(", ", unknown)}\n");
                p.WriteOptionDescriptions(Console.Out);
                Console.WriteLine();
                return 1;
            }

            if (showHelp)
            {
                p.WriteOptionDescriptions(Console.Out);
                Console.WriteLine();
                return 0;
            }

            if (parseOnly)
            {
                var properties = typeof(Config).GetProperties()
                    .OrderBy(p => p.Name)
                    .ToList();

                foreach (var propertyInfo in properties)
                {
                    Console.WriteLine($"{propertyInfo.Name}: {propertyInfo.GetValue(config)}");
                }

                Console.WriteLine();
                return 0;
            }

            var logger = Logger.CreateLogger<Program>();

            if (!config.Validate(out var errors))
            {
                logger.LogError($"Vennligst sjekk programmets instillinger.\n{string.Join("\n", errors)}");
                return 1;
            }

            try
            {
                var service = new Service(config, Logger.CreateLogger<Service>());
                var job = service.PrepareJob();

                if (listOnly)
                {
                    logger.LogDebug($"ListOnly {config}");
                    logger.LogInformation(job.ToString());
                }
                else
                {
                    logger.LogDebug($"Run {config}");
                    service.Run(job);
                }

                return 0;
            }
            catch (Exception ex)
            {
                logger.LogCritical(ex, $"{ex.GetType().Name}: {ex.Message}");
                return 1;
            }
            finally
            {
                Logger.Shutdown();
            }
        }
    }
}
