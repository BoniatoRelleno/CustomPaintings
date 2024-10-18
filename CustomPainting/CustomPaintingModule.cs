using System.Collections.Generic;
using System.IO;
using System.Linq;
using BepInEx.Logging;
using Unity.Netcode;
using UnityEngine;

namespace CustomPainting;

internal class CustomPaintingModule : CustomPaintingModuleBase
{
    private readonly string[] _paintingFiles;
    private System.Random? _randomForTextureSelection;
    private readonly Dictionary<int, int> _paintingValues = new();

    internal CustomPaintingModule(IEnumerable<string> paintingFiles, ManualLogSource logger) : base(logger)
    {
        _paintingFiles = paintingFiles.ToArray();
    }

    internal override void SetMaterialVariantsForPainting(Item paintingItem)
    {
        if (_paintingFiles.Length <= 0) return;

        Logger.LogInfo("Updating textures");
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
            Logger.LogDebug($"File #{i} = {_paintingFiles[i]}");
        }
        Logger.LogInfo("Textures updated");
    }

    internal override void GenerateTextureIndexForPainting(GrabbableObject painting)
    {
        if (!StartOfRound.Instance.IsServer && StartOfRound.Instance.shipHasLanded)
        {
            Logger.LogDebug($"Generating and applying skipped (IsServer = {StartOfRound.Instance.IsServer} shipHasLanded = {StartOfRound.Instance.shipHasLanded})");
            return;
        }

        if (_paintingValues.TryGetValue(painting.GetInstanceID(), out var fileIndex)) 
            return;

        if (_randomForTextureSelection == null)
            SetSeedForTextureIndexGenerator(StartOfRound.Instance.randomMapSeed);

        fileIndex = _randomForTextureSelection!.Next(_paintingFiles.Length);
        _paintingValues[painting.GetInstanceID()] = fileIndex;
        ApplyMaterial(painting, fileIndex);
        Logger.LogInfo($"Generated fileIndex ({fileIndex}) for object {painting.gameObject} ({painting.GetInstanceID()})");
    }

    private void ApplyMaterial(GrabbableObject painting, int fileIndex)
    {
        if (fileIndex < 0)
        {
            Logger.LogError($"Incorrect fileIndex: {fileIndex}");
        }
        else if (fileIndex >= painting.itemProperties.materialVariants.Length)
        {
            Logger.LogError($"FileIndex ({fileIndex}) is greater then available materials ({painting.itemProperties.materialVariants.Length})");
        }
        else
        {
            painting.gameObject.GetComponent<MeshRenderer>().sharedMaterial =
                painting.itemProperties.materialVariants[fileIndex];
            Logger.LogInfo($"Applied fileIndex {fileIndex} texture");
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
                Logger.LogWarning($"Cannot find file index for object {grabbable.gameObject} ({grabbable.GetInstanceID()})");
                continue;
            }

            ApplyMaterial(grabbable, fileIndex);
        }
    }

    internal override void SetSeedForTextureIndexGenerator(int roundSeed)
    {
        var seed = roundSeed + 300;
        Logger.LogDebug($"Used seed {seed}");
        _randomForTextureSelection = new(seed);
    }

    internal override int GetPaintingFileIndex(GrabbableObject painting)
    {
        if (_paintingValues.TryGetValue(painting.GetInstanceID(), out var fileIndex))
        {
            Logger.LogInfo($"FileIndex ({fileIndex}) for object {painting.gameObject} ({painting.GetInstanceID()})");
            return fileIndex;
        }
        Logger.LogWarning($"Dictionary doesn't contain fileIndex for object {painting.gameObject} ({painting.GetInstanceID()})");

        return default;
    }

    internal override void LoadPaintingTextureByIndex(GrabbableObject painting, int fileIndex)
    {
        if (fileIndex < 0)
        {
            Logger.LogError($"Incorrect fileIndex: {fileIndex}");
            return;
        }

        if (fileIndex > _paintingFiles.Length)
        {
            Logger.LogWarning($"FileIndex ({fileIndex}) is greater than files count ({_paintingFiles.Length})");
            return;
        }

        _paintingValues[painting.GetInstanceID()] = fileIndex;

        ApplyMaterial(painting, fileIndex);

        Logger.LogInfo($"Loaded fileIndex ({fileIndex}) for object {painting.gameObject} ({painting.GetInstanceID()})");
    }

    internal override void ClearSelectedTextures()
    {
        _paintingValues.Clear();
        _randomForTextureSelection = null;
    }
}