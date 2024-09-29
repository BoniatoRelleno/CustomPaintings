using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using CustomPainting.Configuration;
using Unity.Netcode;
using UnityEngine;
using Logger = BepInEx.Logging.Logger;

namespace CustomPainting;

internal class CustomPaintingModule
{
    private readonly ManualLogSource _logger;
    private readonly Config _config;
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

    private readonly string[] _paintingFiles;
    private System.Random? _randomForTextureSelection;
    private readonly Dictionary<int, int> _paintingValues = new();

    internal bool HasTextures => _paintingFiles.Any();

    internal CustomPaintingModule(ConfigFile config, string fullModName)
    {
        _logger = Logger.CreateLogSource(fullModName);
        _config = new(config);

        var defaultDirectory = PrepareDefaultDirectory();

        PrepareWebTextures(defaultDirectory);

        var directories = PrepareAdditionalTextureDirectories(defaultDirectory);

        _paintingFiles = GetTextureFiles(directories).ToArray();

        _logger.LogInfo("------- CustomPaintings loaded -------");
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
        directories.AddRange(Directory.GetDirectories(Paths.PluginPath, "LethalPaintings", SearchOption.AllDirectories));

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
                    ? i < DefaultTexturesUrls.Length
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
                    if (i < 9)
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
        for (var i = 1; i < 9; i++) 
            File.Delete($"{directory}/default{i}.png");
    }

    private static IEnumerable<string> GetTextureFiles(IEnumerable<string> directories)
    {
        var files = new List<string>();
        foreach (var directory in directories)
        {
            if (!Directory.Exists(directory)) continue;

            var allPaintingPaths = Directory.GetFiles(directory);
            files.AddRange(allPaintingPaths);
        }

        files.Sort();
        return files;
    }

    internal void PatchPaintingItem(Item paintingItem)
    {
        SetPaintingVariants(paintingItem);
        paintingItem.saveItemVariable = true;
    }

    private void SetPaintingVariants(Item paintingItem)
    {
        if (_paintingFiles.Length <= 0) return;

        _logger.LogInfo("Updating textures");
        var baseMaterialVariant = paintingItem.materialVariants[0];
        paintingItem.materialVariants = _paintingFiles.Select(f =>
            {
                var texture = new Texture2D(2, 2);
                texture.LoadImage(File.ReadAllBytes(f));

                return new Material(baseMaterialVariant)
                {
                    mainTexture = texture
                };
            })
            .ToArray();

        for (var i = 0; i < _paintingFiles.Length; i++)
        {
            _logger.LogDebug($"File #{i} = {_paintingFiles[i]}");
        }
        _logger.LogInfo("Textures updated");
    }

    internal void GenerateTextureIndexForPainting(GrabbableObject painting)
    {
        if (!StartOfRound.Instance.IsServer && StartOfRound.Instance.shipHasLanded)
        {
            _logger.LogDebug($"Generating and applying skipped (IsServer = {StartOfRound.Instance.IsServer} shipHasLanded = {StartOfRound.Instance.shipHasLanded})");
            return;
        }

        if (_paintingValues.TryGetValue(painting.GetInstanceID(), out var fileIndex)) 
            return;

        if (_randomForTextureSelection == null)
            SetSeedForTextureIndexGenerator(StartOfRound.Instance.randomMapSeed);

        fileIndex = _randomForTextureSelection!.Next(_paintingFiles.Length);
        _paintingValues[painting.GetInstanceID()] = fileIndex;
        ApplyMaterial(painting, fileIndex);
        _logger.LogInfo($"Generated fileIndex ({fileIndex}) for object {painting.gameObject} ({painting.GetInstanceID()})");
    }

    private void ApplyMaterial(GrabbableObject painting, int fileIndex)
    {
        if (fileIndex < 0)
        {
            _logger.LogError($"Incorrect fileIndex: {fileIndex}");
        }
        else if (fileIndex >= painting.itemProperties.materialVariants.Length)
        {
            _logger.LogError($"FileIndex ({fileIndex}) is greater then available materials ({painting.itemProperties.materialVariants.Length})");
        }
        else
        {
            painting.gameObject.GetComponent<MeshRenderer>().sharedMaterial =
                painting.itemProperties.materialVariants[fileIndex];
            _logger.LogInfo($"Applied fileIndex {fileIndex} texture");
        }
    }

    internal void ApplyTexturesForPaintingsInSpawnedScrap(NetworkObjectReference[] spawnedScrap)
    {
        foreach (var networkObjectReference in spawnedScrap)
        {
            if (!networkObjectReference.TryGet(out var networkObject)) continue;

            var grabbable = networkObject.GetComponent<GrabbableObject>();

            if (!grabbable.IsPainting()) continue;

            if (!_paintingValues.TryGetValue(grabbable.GetInstanceID(), out var fileIndex))
            {
                _logger.LogWarning($"Cannot find file index for object {grabbable.gameObject} ({grabbable.GetInstanceID()})");
                continue;
            }

            ApplyMaterial(grabbable, fileIndex);
        }
    }

    internal void SetSeedForTextureIndexGenerator(int roundSeed)
    {
        var seed = roundSeed + 300;
        _logger.LogDebug($"Used seed {seed}");
        _randomForTextureSelection = new(seed);
    }

    internal int GetPaintingFileIndex(GrabbableObject painting)
    {
        if (_paintingValues.TryGetValue(painting.GetInstanceID(), out var fileIndex))
        {
            _logger.LogInfo($"FileIndex ({fileIndex}) for object {painting.gameObject} ({painting.GetInstanceID()})");
            return fileIndex;
        }
        _logger.LogWarning($"Dictionary doesn't contain fileIndex for object {painting.gameObject} ({painting.GetInstanceID()})");

        return default;
    }

    internal void LoadPaintingTextureByIndex(GrabbableObject painting, int fileIndex)
    {
        if (fileIndex < 0)
        {
            _logger.LogError($"Incorrect fileIndex: {fileIndex}");
            return;
        }

        if (fileIndex > _paintingFiles.Length)
        {
            _logger.LogWarning($"FileIndex ({fileIndex}) is greater than files count ({_paintingFiles.Length})");
            return;
        }

        _paintingValues[painting.GetInstanceID()] = fileIndex;

        ApplyMaterial(painting, fileIndex);

        _logger.LogInfo($"Loaded fileIndex ({fileIndex}) for object {painting.gameObject} ({painting.GetInstanceID()})");
    }

    internal void ClearSelectedTextures()
    {
        _paintingValues.Clear();
        _randomForTextureSelection = null;
    }
}