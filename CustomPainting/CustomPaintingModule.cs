using System.Collections.Generic;
using System.IO;
using System.Linq;
using BepInEx.Logging;
using Unity.Netcode;
using UnityEngine;

namespace CustomPainting;

internal class CustomPaintingModule : CustomPaintingModuleBase
{
    private readonly ManualLogSource _logger;

    private readonly string[] _paintingFiles;
    private System.Random? _randomForTextureSelection;
    private readonly Dictionary<int, int> _paintingValues = new();

    internal CustomPaintingModule(IEnumerable<string> paintingFiles, ManualLogSource logger)
    {
        _paintingFiles = paintingFiles.ToArray();
        _logger = logger;
    }

    internal override void PatchPaintingItemCore(Item paintingItem)
    {
        SetPaintingVariants(paintingItem);
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

    internal override void GenerateTextureIndexForPainting(GrabbableObject painting)
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

    internal override void ApplyTexturesForPaintingsInSpawnedScrap(NetworkObjectReference[] spawnedScrap)
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

    internal override void SetSeedForTextureIndexGenerator(int roundSeed)
    {
        var seed = roundSeed + 300;
        _logger.LogDebug($"Used seed {seed}");
        _randomForTextureSelection = new(seed);
    }

    internal override int GetPaintingFileIndex(GrabbableObject painting)
    {
        if (_paintingValues.TryGetValue(painting.GetInstanceID(), out var fileIndex))
        {
            _logger.LogInfo($"FileIndex ({fileIndex}) for object {painting.gameObject} ({painting.GetInstanceID()})");
            return fileIndex;
        }
        _logger.LogWarning($"Dictionary doesn't contain fileIndex for object {painting.gameObject} ({painting.GetInstanceID()})");

        return default;
    }

    internal override void LoadPaintingTextureByIndex(GrabbableObject painting, int fileIndex)
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

    internal override void ClearSelectedTextures()
    {
        _paintingValues.Clear();
        _randomForTextureSelection = null;
    }
}