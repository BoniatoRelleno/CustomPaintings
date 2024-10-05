using Unity.Netcode;

namespace CustomPainting;

internal abstract class CustomPaintingModuleBase
{
    internal void PatchPaintingItem(Item paintingItem)
    {
        paintingItem.saveItemVariable = true;
        PatchPaintingItemCore(paintingItem);
    }

    internal abstract void PatchPaintingItemCore(Item paintingItem);

    internal abstract void GenerateTextureIndexForPainting(GrabbableObject painting);

    internal abstract void ApplyTexturesForPaintingsInSpawnedScrap(NetworkObjectReference[] spawnedScrap);

    internal abstract void SetSeedForTextureIndexGenerator(int roundSeed);

    internal abstract int GetPaintingFileIndex(GrabbableObject painting);

    internal abstract void LoadPaintingTextureByIndex(GrabbableObject painting, int fileIndex);

    internal abstract void ClearSelectedTextures();
}