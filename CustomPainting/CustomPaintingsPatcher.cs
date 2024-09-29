﻿using System.Linq;
using BepInEx;
using HarmonyLib;
using Unity.Netcode;

namespace CustomPainting;

[BepInPlugin(ModGuid, ModName, ModVersion)]
[BepInDependency("com.sigurd.csync", "5.0.1")]
public class CustomPaintingsPatcher : BaseUnityPlugin
{
    internal const string ModGuid = "Boniato.CustomPaintings";
    private const string ModName = "CustomPaintings";
    private const string ModVersion = "2.0.0";

    private readonly Harmony _harmony = new(ModGuid);
    internal static CustomPaintingsPatcher? Instance;
    private static CustomPaintingModule? _module;

    void Awake()
    {
        Instance ??= this;
        _module ??= new(Config, ModGuid);

        if (_module.HasTextures)
        {
            _harmony.PatchAll(typeof(CustomPaintingsPatcher));
        }
    }

    [HarmonyPatch(typeof(StartOfRound), "Start")]
    [HarmonyPrefix]
    private static void FindAndPatchPaintingItemWhenGameLoaded(StartOfRound __instance)
    {
        var paintingItem = __instance.allItemsList.itemsList.First(i => i.itemName == "Painting");
        _module?.PatchPaintingItem(paintingItem);
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

    [HarmonyPatch(typeof(GameNetworkManager), nameof(GameNetworkManager.ResetGameValuesToDefault))]
    [HarmonyPostfix]
    public static void ClearSelectedTexturesAfterEndGame()
    {
        _module?.ClearSelectedTextures();
    }
}