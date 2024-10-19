using System.Linq;
using BepInEx;
using CustomPainting.Configuration;
using HarmonyLib;
using Unity.Netcode;

namespace CustomPainting;

[BepInPlugin(ModGuid, ModName, ModVersion)]
public class CustomPaintingsPatcher : BaseUnityPlugin
{
    private const string ModGuid = "Boniato.CustomPaintings";
    private const string ModName = "CustomPaintings";
    private const string ModVersion = "2.0.0";

    private readonly Harmony _harmony = new(ModGuid);
    private static CustomPaintingsPatcher? Instance;
    private static CustomPaintingsModuleBase? _module;
    private static Config? _config;

    void Awake()
    {
        Instance ??= this;
        var logger = BepInEx.Logging.Logger.CreateLogSource(ModGuid);
        _config = new Config(Config);
        var textures = new TextureFilesLoader(_config, logger).Load();

        if (textures.Any())
        {
            _module = new CustomPaintingsModule(textures, logger);
            logger.LogDebug("Used real module");
        }
        else
        {
            _module = new DummyCustomPaintingsModule(logger);
            logger.LogDebug("Used dummy module");
        }

        _harmony.PatchAll(typeof(CustomPaintingsPatcher));
        logger.LogInfo("------- CustomPaintings loaded -------");
    }

    [HarmonyPatch(typeof(StartOfRound), "Start")]
    [HarmonyPrefix]
    private static void FindAndPatchPaintingItemWhenGameLoaded(StartOfRound __instance)
    {
        var shouldPatchPainting = _module?.CheckLobbyOrPatchIt() ?? false;

        var paintingItem = __instance.allItemsList.itemsList.First(i => i.itemName == "Painting");
        if (shouldPatchPainting)
        {
            _module?.EnableSavingDataForPainting(paintingItem);
        }
        _module?.SetMaterialVariantsForPainting(paintingItem);
    }

    [HarmonyPatch(typeof(GrabbableObject), "Start")]
    [HarmonyPrefix]
    private static void GenerateTextureIndexForPaintingOnObjectStart(GrabbableObject __instance)
    {
        if (!__instance.IsPainting()) return;
        _module?.GenerateTextureIndexForPainting(__instance);
    }

    [HarmonyPatch(typeof(RoundManager), nameof(RoundManager.SyncScrapValuesClientRpc))]
    [HarmonyPostfix]
    private static void ApplyTexturesForPaintingsInSpawnedScrapOnSyncScrapValues(NetworkObjectReference[] spawnedScrap)
    {
        _module?.ApplyTexturesForPaintingsInSpawnedScrap(spawnedScrap);
    }

    [HarmonyPatch(typeof(RoundManager), nameof(RoundManager.GenerateNewLevelClientRpc))]
    [HarmonyPrefix]
    private static void SetSeedForTextureIndexGeneratorOnLevelStart(int randomSeed)
    {
        _module?.SetSeedForTextureIndexGenerator(randomSeed);
    }

    [HarmonyPatch(typeof(GrabbableObject), nameof(GrabbableObject.GetItemDataToSave))]
    [HarmonyPostfix]
    private static void GetPaintingFileIndexToSave(GrabbableObject __instance, ref int __result)
    {
        if (!__instance.IsPainting()) return;
        __result = _module?.GetPaintingFileIndex(__instance) ?? __result;
    }

    [HarmonyPatch(typeof(GrabbableObject), nameof(GrabbableObject.LoadItemSaveData))]
    [HarmonyPostfix]
    private static void LoadPaintingTextureBySaveData(GrabbableObject __instance, int saveData)
    {
        if (!__instance.IsPainting()) return;
        _module?.LoadPaintingTextureByIndex(__instance, saveData);
    }

    [HarmonyPatch(typeof(GameNetworkManager), "ResetGameValuesToDefault")]
    [HarmonyPostfix]
    public static void ClearSelectedTexturesAfterEndGame()
    {
        _module?.ClearSelectedTextures();
    }
}