using BepInEx.Configuration;
using GameNetcodeStuff;
using HarmonyLib;
using LethalLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Netcode;

namespace CustomPaintings.Configuration
{
    [Serializable]
    public class Config : SyncedInstance<Config>
    {
        public string directory;

        public string customUrls;

        public bool defaultPaintings;

        public bool forceDownload;

        public int maxTextures;

        public Config(ConfigFile cfg)
        {
            InitInstance(this);

            directory = cfg.Bind(
                "General",
                "Directory",
                "plugins/CustomPaintings",
                "Directory in which the mod will search for the paintings (using BepInEx as root, use / as separator)"
            ).Value;

            customUrls = cfg.Bind(
                    "General",
                    "Custom Urls",
                    "",
                    "Custom urls of the images to download them as default (separate them with commas, for example: https://i.imgur.com/ePiClDl.png,https://i.imgur.com/yZCdjxh.png)"
            ).Value;

            defaultPaintings = cfg.Bind(
                    "General",
                    "Default Paintings",
                    true,
                    "Enable it to use the default paintings of famous works"
            ).Value;

            forceDownload = cfg.Bind(
                    "General",
                    "Force Download",
                    false,
                    "Enable it to download the url images in every launch (if it is false, the mod will download the textures only when creates the directory)"
            ).Value;

            maxTextures = cfg.Bind(
                    "General",
                    "Max Textures",
                    5,
                    "Number of textures per game to prevent duplicated ones (try to avoid high numbers)"
            ).Value;
        }

        public static void RequestSync()
        {
            if (!IsClient) return;

            using FastBufferWriter stream = new(IntSize, Allocator.Temp);
            MessageManager.SendNamedMessage("CustomPaintings_OnRequestConfigSync", 0uL, stream);
        }

        public static void OnRequestSync(ulong clientId, FastBufferReader _)
        {
            if (!IsHost) return;

            Plugin.logger.LogInfo($"Config sync request received from client: {clientId}");

            byte[] array = SerializeToBytes(Instance);
            int value = array.Length;

            using FastBufferWriter stream = new(value + IntSize, Allocator.Temp);

            try
            {
                stream.WriteValueSafe(in value, default);
                stream.WriteBytesSafe(array);

                MessageManager.SendNamedMessage("CustomPaintings_OnReceiveConfigSync", clientId, stream);
            }
            catch (Exception e)
            {
                Plugin.logger.LogInfo($"Error occurred syncing config with client: {clientId}\n{e}");
            }
        }

        public static void OnReceiveSync(ulong _, FastBufferReader reader)
        {
            if (!reader.TryBeginRead(IntSize))
            {
                Plugin.logger.LogError("Config sync error: Could not begin reading buffer.");
                return;
            }

            reader.ReadValueSafe(out int val, default);
            if (!reader.TryBeginRead(val))
            {
                Plugin.logger.LogError("Config sync error: Host could not sync.");
                return;
            }

            byte[] data = new byte[val];
            reader.ReadBytesSafe(ref data, val);

            SyncInstance(data);

            Plugin.logger.LogInfo("Successfully synced config with host.");
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(PlayerControllerB), "ConnectClientToPlayerObject")]
        public static void InitializeLocalPlayer()
        {
            if (IsHost)
            {
                MessageManager.RegisterNamedMessageHandler("CustomPaintings_OnRequestConfigSync", OnRequestSync);
                Synced = true;
                return;
            }

            Synced = false;
            MessageManager.RegisterNamedMessageHandler("CustomPaintings_OnReceiveConfigSync", OnReceiveSync);
            RequestSync();
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(GameNetworkManager), "StartDisconnect")]
        public static void PlayerLeave()
        {
            Config.RevertSync();
        }
    }
}
