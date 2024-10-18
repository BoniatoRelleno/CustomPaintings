using System;
using System.Collections.Generic;
using BepInEx;
using System.IO;
using System.Linq;
using System.Net;
using BepInEx.Logging;
using CustomPainting.Configuration;

namespace CustomPainting;

internal class TextureFilesLoader
{
    private const string DefaultTextureNameFormat = "default{0}.png";
    private static readonly string[] DefaultTexturesUrls =
    [
        "https://i.imgur.com/ePiClDl.png",
        "https://i.imgur.com/yZCdjxh.png",
        "https://i.imgur.com/bIb8wXG.png",
        "https://i.imgur.com/kl1QiwG.png",
        "https://i.imgur.com/2kHSzn6.png",
        "https://i.imgur.com/rvdZYBH.png",
        "https://i.imgur.com/tgJinSI.png",
        "https://i.imgur.com/ZullTxg.png"
    ];

    private readonly ManualLogSource _logger;
    private readonly Config _config;

    internal TextureFilesLoader(Config config, ManualLogSource logger)
    {
        _logger = logger;
        _config = config;
    }

    internal IEnumerable<string> Load()
    {

        var defaultDirectory = PrepareDefaultDirectory();

        PrepareWebTextures(defaultDirectory);

        var directories = PrepareAdditionalTextureDirectories(defaultDirectory);

        return GetTextureFiles(directories).ToArray();
    }

    private IEnumerable<string> PrepareUrls()
    {
        var urls = new List<string>();
        if (_config.DefaultPaintings.Value)
            urls.AddRange(DefaultTexturesUrls);

        if (!string.IsNullOrEmpty(_config.CustomUrls.Value))
            urls.AddRange(_config.CustomUrls.Value.Split(',').Select(v => v.Trim()).ToArray());

        return urls;
    }

    private static string PrepareDefaultDirectory()
    {
        var defaultDirectory = Path.GetFullPath(Path.Combine(Paths.PluginPath, Config.DefaultDirectory));

        if (!Directory.Exists(defaultDirectory))
        {
            Directory.CreateDirectory(defaultDirectory);
        }
        return defaultDirectory;
    }

    private IEnumerable<string> PrepareAdditionalTextureDirectories(string defaultDirectory)
    {
        // Obtain paintings from mods installed by mod manager
        var directories = Directory.GetDirectories(Paths.PluginPath, Config.DefaultDirectory, SearchOption.AllDirectories)
            .ToList();

        // Add paintings made for LethalPainting mod
        directories.AddRange(Directory.GetDirectories(Paths.PluginPath, "paintings", SearchOption.AllDirectories));

        directories.AddRange(
            _config.AdditionalDirectories.Value.Split(',')
                .Select(d => d.Trim())
                .Select(d => Path.IsPathRooted(d) ? d : Path.Combine(Paths.BepInExRootPath, d))
        );

        directories = directories
            .Select(Path.GetFullPath)
            .Where(d => !d.Equals(defaultDirectory, StringComparison.InvariantCultureIgnoreCase))
            .Distinct()
            .OrderBy(d => d)
            .ToList();

        directories.Insert(0, defaultDirectory);
        return directories;
    }

    private void PrepareWebTextures(string directoryDownloadTo)
    {
        var shouldDownload = _config.ForceDownload.Value;
        if (_config.DefaultPaintings.Value)
            shouldDownload |= !CheckDefaultTextureExisting(directoryDownloadTo);
        else
            DeleteDefaultTextures(directoryDownloadTo);

        var urls = PrepareUrls();
        shouldDownload |= Directory.GetFiles(directoryDownloadTo).Length < urls.Count();

        if (shouldDownload)
            DownloadTextures(directoryDownloadTo, urls);
    }

    private static bool CheckDefaultTextureExisting(string directory)
    {
        for (var i = 0; i < DefaultTexturesUrls.Length; i++)
        {
            if (!File.Exists(Path.Combine(directory, string.Format(DefaultTextureNameFormat, i))))
                return false;
        }

        return true;
    }

    private void DownloadTextures(string directory, IEnumerable<string> urls)
    {
        var i = 1;
        foreach (var url in urls)
        {
            var request = (HttpWebRequest)WebRequest.Create(url);
            var response = (HttpWebResponse)request.GetResponse();
            if (response.StatusCode == HttpStatusCode.OK)
            {
                var fileName = _config.DefaultPaintings.Value
                    ? i <= DefaultTexturesUrls.Length
                        ? $"{directory}/default{i}.png"
                        : $"{directory}/custom{i - DefaultTexturesUrls.Length - 1}.png"
                    : $"{directory}/custom{i}.png";

                using var fileStream = new FileStream(fileName, FileMode.Create);
                response.GetResponseStream()?.CopyTo(fileStream);
            }
            else
            {
                if (_config.DefaultPaintings.Value)
                {
                    if (i <= DefaultTexturesUrls.Length)
                    {
                        _logger.LogWarning($"Error downloading default image {i}");
                    }
                    else
                    {
                        _logger.LogWarning($"Error downloading custom image {i - 8}");
                    }
                }
                else
                {
                    _logger.LogWarning($"Error downloading custom image {i}");
                }
            }
            i++;
        }
    }

    private static void DeleteDefaultTextures(string directory)
    {
        for (var i = 1; i <= DefaultTexturesUrls.Length; i++)
            File.Delete($"{directory}/default{i}.png");
    }

    private static IEnumerable<string> GetTextureFiles(IEnumerable<string> directories)
    {
        var files = new List<string>();
        var supportedFileTypes = new[] { ".png", ".jpg", ".jpeg", ".bmp" };
        foreach (var directory in directories)
        {
            if (!Directory.Exists(directory)) continue;

            foreach (var file in Directory.EnumerateFiles(directory))
                if (supportedFileTypes.Any(t => file.EndsWith(t)))
                    files.Add(file);
        }

        files.Sort();
        return files;
    }
}