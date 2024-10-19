using BepInEx.Logging;
using Unity.Netcode;

namespace CustomPainting;

internal abstract class CustomPaintingsModuleBase(ManualLogSource logger)
{
    protected readonly ManualLogSource Logger = logger;

    internal bool CheckLobbyOrPatchIt()
    {
        var shouldPatchPainting = false;
        var lobby = GameNetworkManager.Instance?.currentLobby;
        if (lobby == null)
        {
            Logger.LogDebug("Lobby not created");
        }
        else
        {
            if (StartOfRound.Instance.IsServer)
            {
                lobby.Value.SetData("CustomPaintings", bool.TrueString);
                shouldPatchPainting = true;
            }
            else
            {
                var value = lobby.Value.GetData("CustomPaintings");
                if (value == bool.TrueString)
                {
                    shouldPatchPainting = true;
                    Logger.LogDebug("Mod enabled");
                }
                else
                {
                    Logger.LogDebug("Host doesn't use mod");
                }
            }
        }

        return shouldPatchPainting;
    }

    internal void EnableSavingDataForPainting(Item paintingItem)
    {
        paintingItem.saveItemVariable = true;
    }

    internal abstract void SetMaterialVariantsForPainting(Item paintingItem);

    internal abstract void GenerateTextureIndexForPainting(GrabbableObject painting);

    internal abstract void ApplyTexturesForPaintingsInSpawnedScrap(NetworkObjectReference[] spawnedScrap);

    internal abstract void SetSeedForTextureIndexGenerator(int roundSeed);

    internal abstract int GetPaintingFileIndex(GrabbableObject painting);

    internal abstract void LoadPaintingTextureByIndex(GrabbableObject painting, int fileIndex);

    internal abstract void ClearSelectedTextures();
}