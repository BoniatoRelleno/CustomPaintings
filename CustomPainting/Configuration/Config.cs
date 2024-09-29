using System;
using System.Runtime.Serialization;
using BepInEx.Configuration;
using CSync.Extensions;
using CSync.Lib;

namespace CustomPainting.Configuration
{
    public class Config : SyncedConfig2<Config>
    {
        internal const string DefaultDirectory = "CustomPaintings";

        public ConfigEntry<string> AdditionalDirectories;

        [Obsolete($"Use now {nameof(AdditionalDirectories)}")]
        public ConfigEntry<string> Directory;

        [DataMember]
        public SyncedEntry<string> CustomUrls;

        [DataMember]
        public SyncedEntry<bool> DefaultPaintings;

        [DataMember]
        public SyncedEntry<bool> ForceDownload;
        
        public Config(ConfigFile cfg) : base(CustomPaintingsPatcher.ModGuid)
        {
            ConfigManager.Register(this);

            Directory = cfg.Bind(
                new("General", "Directory"),
                string.Empty,
                new($"Deprecated! Use {nameof(AdditionalDirectories)} now. Directory in which the mod will search for the paintings (using BepInEx as root, use / as separator)")
            );

            AdditionalDirectories = cfg.Bind(
                new ConfigDefinition("General", "Additional directories"),
                string.IsNullOrEmpty(Directory.Value) ? $"plugins/{DefaultDirectory}" : Directory.Value,
                new ConfigDescription("List of directories split by semi for painting textures search"));


            CustomUrls = cfg.BindSyncedEntry(
                new ("General", "Custom Urls"),
                string.Empty,
                new ConfigDescription("Custom urls of the images to download them as default (separate them with commas, for example: https://i.imgur.com/ePiClDl.png,https://i.imgur.com/yZCdjxh.png)")
            );

            DefaultPaintings = cfg.BindSyncedEntry(
                    new("General", "Default Paintings"),
                    true,
                    new ConfigDescription("Enable it to use the default paintings of famous works")
            );

            ForceDownload = cfg.BindSyncedEntry(
                    new("General", "Force Download"),
                    false,
                    new ConfigDescription("Enable it to download the url images in every launch (if it is false, the mod will download the textures only when creates the directory)")
            );
        }
    }
}
