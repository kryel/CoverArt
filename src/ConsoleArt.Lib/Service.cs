using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace ConsoleArt.Lib
{
    public class Service
    {
        private readonly Config _config;
        private readonly ILogger<Service> _logger;

        private readonly Regex _searchPattern;
        private readonly HashSet<string> _fileTypes;

        public Service(Config config, ILogger<Service> logger)
        {
            _config = config;
            _logger = logger;

            _searchPattern = new Regex(_config.SearchPattern, RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline);


            _fileTypes = _config.FileTypes
                .SelectMany(f => new[] { f.TrimStart('.'), '.' + f.TrimStart('.') })
                .Select(f => f.ToUpperInvariant())
                .ToHashSet();
        }

        public Job PrepareJob()
        {
            var artists = Directory.EnumerateDirectories(_config.Input)
                .OrderBy(x => x)
                .Select(f => new DirectoryInfo(f).Name);

            var job = new Job
            {
                Root = _config.Input,
                Artists = new List<Artist>()
            };

            foreach (var artistName in artists)
            {
                var artist = new Artist
                {
                    Name = artistName,
                    Albums = new List<Album>()
                };

                job.Artists.Add(artist);

                var artistDir = Path.Combine(_config.Input, artistName);
                var albums = Directory.EnumerateDirectories(artistDir)
                    .OrderBy(x => x)
                    .Select(f => new DirectoryInfo(f).Name);

                foreach (var albumName in albums)
                {
                    var album = new Album
                    {
                        Name = albumName,
                        CoverArt = new List<string>()
                    };

                    artist.Albums.Add(album);

                    var albumDir = Path.Combine(artistDir, albumName);

                    var list = Directory.EnumerateFiles(albumDir)
                        .Select(f => new DirectoryInfo(f).Name)
                        .OrderBy(x => x)
                        .Where(f => _searchPattern.IsMatch(f) && _fileTypes.Contains(Path.GetExtension(f).ToUpperInvariant()))
                        .ToList();

                    album.CoverArt = list;
                }
            }

            return job;
        }

        public void Run(Job job)
        {
            if (!job.Artists.Any())
            {
                _logger.LogInformation($"Ingen artister funnet i '{job.Root}'");
                return;
            }

            foreach (var artist in job.Artists)
            {
                Console.WriteLine();
                _logger.LogInformation("Neste artist: '{0}'", artist.Name);

                if (!GetConfirmation("Fortsette?"))
                {
                    return;
                }

                foreach (var album in artist.Albums)
                {
                    Console.WriteLine();

                    var albumDescription = $"Album: '{album.Name}' ('{artist.Name}')";
                    _logger.LogInformation(albumDescription);

                    string coverArt;

                    if (album.CoverArt.Count == 0)
                    {
                        var choice = GetConfirmation("Ingen passende filer. Hopp over og fortsett med neste?");
                        if (choice)
                        {
                            continue;
                        }

                        _logger.LogDebug("Stoppet fordi ingen passende filer ble funnet");
                        return;
                    }

                    if (album.CoverArt.Count > 1)
                    {
                        var choice = GetOption("Flere filer passer kriteriet", album.CoverArt);
                        coverArt = album.CoverArt[choice];
                        _logger.LogDebug($"Valgte: '{coverArt}' fra '{string.Join("', '", album.CoverArt)}'");
                    }
                    else
                    {
                        coverArt = album.CoverArt.First();
                    }

                    var currentFolder = Path.Combine(job.Root, artist.Name, album.Name);

                    var newName = _searchPattern.Replace(coverArt, _config.ResultFileName);

                    var orig = Path.Combine(currentFolder, coverArt);
                    var copy = Path.ChangeExtension(Path.Combine(currentFolder, newName), Path.GetExtension(coverArt));

                    var conflicts = Directory.EnumerateFiles(currentFolder, $"{_config.ResultFileName}.*")
                        .Where(f => _fileTypes.Contains(Path.GetExtension(f.ToUpperInvariant())))
                        .ToList();

                    var createFile = true;
                    var overwrite = false;

                    if (conflicts.Any())
                    {
                        _logger.LogInformation($"Det finnes allerede kandidater i mappen {currentFolder}:\n{string.Join("\n", conflicts)}");

                        var exists = File.Exists(copy);
                        var options = new List<string>
                        {
                            "Stopp",
                            "Ikke skriv ny fil",
                            exists
                                ? "(!) Skriv over den eksisterende filen"
                                : "Opprett ny fil (andre kandidater vil være urørt)"
                        };

                        var option = GetOption("Vil du skrive over?", options);
                        switch (option)
                        {
                            case 0:
                                _logger.LogDebug($"Stopper ved {albumDescription} fordi filen allerede finnes");
                                return;
                            case 1:
                                createFile = false;
                                overwrite = false;
                                _logger.LogInformation($"Hopper over");
                                break;
                            case 2:
                                createFile = true;
                                overwrite = exists;
                                _logger.LogDebug($"Skriver over");
                                break;
                            default:
                                throw new ArgumentException($"Ukjent valg gjort: {option}");
                        }
                    }

                    _logger.LogDebug($"Valgte: {nameof(createFile)}: {createFile}, {nameof(overwrite)}: {overwrite}");

                    if (createFile)
                    {
                        File.Copy(orig, copy, overwrite);
                        _logger.LogInformation($"Kopierte {orig} til {copy}");
                    }
                }

                var oldArtist = Path.Combine(job.Root, artist.Name);
                var newArtist = Path.Combine(_config.Output, artist.Name);

                Console.WriteLine();
                _logger.LogInformation($"Artisten '{artist.Name}' er behandlet ferdig.");

                if (Directory.Exists(newArtist))
                {
                    _logger.LogWarning($"Kan ikke flytte '{oldArtist}' til '{newArtist}' fordi katalogen finnes allerede");
                }
                else
                {
                    if (GetConfirmation($"Vil du flytte '{oldArtist}' til '{newArtist}'?"))
                    {
                        _logger.LogInformation($"Flytter '{oldArtist}' til '{newArtist}'");
                        Directory.Move(oldArtist, newArtist);
                    }
                    else
                    {
                        _logger.LogDebug($"Flyttet IKKE '{oldArtist}' til '{newArtist}'");
                    }
                }
            }
        }

        private bool GetConfirmation(string message)
        {
            var affirmatives = new[] { "Y", "J" };
            var negatives = new[] { "N" };

            while (true)
            {
                Console.WriteLine(message);
                Console.WriteLine("[J]a / [N]ei");
                Console.Write("> ");

                var line = Console.ReadLine()?.ToUpperInvariant();

                if (line is null)
                {
                    throw new NotSupportedException("Feil oppsto: line is null");
                }

                if (affirmatives.Contains(line))
                {
                    return true;
                }

                if (negatives.Contains(line))
                {
                    return false;
                }

                Console.WriteLine();
                Console.Error.WriteLine($"Feil input: '{line}'. Forventet 'J' eller 'N'");
            }
        }

        public int GetOption(string message, List<string> options)
        {
            while (true)
            {

                Console.WriteLine(message);
                Console.WriteLine("Vennlist velg en av:");
                for (var i = 0; i < options.Count; i++)
                {
                    Console.WriteLine($"[{i + 1}]: {options[i]}");
                }

                Console.Write("> ");
                var line = Console.ReadLine()?.ToUpperInvariant();

                if (int.TryParse(line, out var choice))
                {
                    if (choice > 0 && choice <= options.Count)
                    {
                        return choice - 1;
                    }
                }

                Console.WriteLine();
                Console.Error.WriteLine($"Feil input: '{line}'. Forventet et tall mellom 0 og {options.Count}");
            }
        }
    }
}
