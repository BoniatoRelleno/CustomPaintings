using BepInEx.Logging;
using Unity.Netcode;

namespace CustomPainting;

internal class DummyCustomPaintingModule(ManualLogSource logger) : CustomPaintingModuleBase(logger)
{
    internal override void SetMaterialVariantsForPainting(Item paintingItem)
    {
    }

    internal override void GenerateTextureIndexForPainting(GrabbableObject painting)
    {
    }

    internal override void ApplyTexturesForPaintingsInSpawnedScrap(NetworkObjectReference[] spawnedScrap)
    {
    }

    internal override void SetSeedForTextureIndexGenerator(int roundSeed)
    {
    }

    internal override int GetPaintingFileIndex(GrabbableObject painting)
    {
        return default;
    }

    internal override void LoadPaintingTextureByIndex(GrabbableObject painting, int fileIndex)
    {
    }

    internal override void ClearSelectedTextures()
    {
    }
}