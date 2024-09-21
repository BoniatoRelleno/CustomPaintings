using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Networking;
using static System.Net.Mime.MediaTypeNames;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Reflection;
using CustomPaintings.Configuration;
using System.Drawing;

namespace CustomPaintings
{
    [BepInPlugin(modGUID, modName, modVersion)]
    public class CustomPaintings : BaseUnityPlugin
    {
        private const string modGUID = "Boniato.CustomPaintings";
        private const string modName = "CustomPaintings";
        private const string modVersion = "1.2.0";

        private readonly Harmony harmony = new Harmony(modGUID);

        internal static CustomPaintings instance;

        internal static ManualLogSource mls;

        public static readonly List<string> paintingFiles = new List<string>();

        public static System.Random random;

        private string separator = "/";

        
        public static Config cfg { get; internal set; }

        public String[] urls = new string[0];


        void Awake()
        {
            if (instance == null)
            {
                instance = this;
            }
            mls = BepInEx.Logging.Logger.CreateLogSource(modGUID);

            cfg = new(Config);

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                separator = "\\";
            }

            //list of textures to download

            if (cfg.defaultPaintings)
            {
                urls = new string[] {
                    "https://i.imgur.com/ePiClDl.png",
                    "https://i.imgur.com/yZCdjxh.png",
                    "https://i.imgur.com/bIb8wXG.png",
                    "https://i.imgur.com/kl1QiwG.png",
                    "https://i.imgur.com/2kHSzn6.png",
                    "https://i.imgur.com/rvdZYBH.png",
                    "https://i.imgur.com/tgJinSI.png",
                    "https://i.imgur.com/ZullTxg.png"
                };
            }
            if (!cfg.customUrls.Equals(""))
            {
                urls = urls.AddRangeToArray(cfg.customUrls.Split(','));
            }

            //directory

            String[] prepath = new String[] { Paths.BepInExRootPath };
            String[] path = prepath.AddRangeToArray(cfg.directory.Split('/'));
            String directory = Path.Combine(path);

            //download textures in directory

            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
                mls.LogInfo("Created directory at " + directory);
                if (!cfg.forceDownload)
                {
                    DownloadTextures(directory);
                }
            } else
            {
                if (!cfg.defaultPaintings)
                {
                    DeleteDefaultTextures(directory);
                }
            }
            if (cfg.forceDownload)
            {
                DownloadTextures(directory);
            }

            //obtain textures from directory

            String[] allPaintingPaths = Directory.GetFiles(directory);
            if (allPaintingPaths.Length == 0)
            {
                mls.LogWarning("No paintings found");
            } else
            {
                paintingFiles.AddRange(allPaintingPaths);
                harmony.PatchAll(typeof(CustomPaintings));
            }
            mls.LogInfo("------- CustomPaintings loaded -------");
        }

        [HarmonyPatch(typeof(GrabbableObject), "Start")]
        [HarmonyPostfix]
        private static void PaintingPatch(GrabbableObject __instance)
        {
            if (__instance.itemProperties.itemName == "Painting")
            {
                random = new System.Random((int)((NetworkBehaviour)__instance).NetworkObjectId);

                UpdateTextures(paintingFiles, __instance);
            }
        }

        private static void UpdateTextures(IReadOnlyList<string> files, GrabbableObject __instance)
        {
            if (files.Count != 0)
            {
                //mls.LogInfo("Updating textures");
                if (cfg.maxTextures > files.Count)
                {
                    cfg.maxTextures = files.Count;
                }
                Material[] materials = new Material[cfg.maxTextures];
                List<int> disabled = new List<int>();
                disabled.Clear();
                for (int i = 0; i < materials.Length; i++)
                {
                    materials[i] = new Material(__instance.itemProperties.materialVariants[0]);
                    int index;
                    do
                    {
                        index = random.Next(files.Count);
                    } while (disabled.Contains(index));
                    disabled.Add(index);
                    Texture2D texture = new Texture2D(2, 2);
                    ImageConversion.LoadImage(texture, File.ReadAllBytes(files[index]));
                    materials[i].mainTexture = texture;
                }
                __instance.itemProperties.materialVariants = materials;
                //mls.LogInfo("Textures updated");
            }
        }

        private void DownloadTextures(String directory)
        {
            int i = 1;
            foreach (String url in urls)
            {
                
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    System.Drawing.Image image = System.Drawing.Image.FromStream(response.GetResponseStream());
                    if (cfg.defaultPaintings)
                    {
                        if (i < 9)
                        {
                            image.Save(directory + separator + "default" + i + ".png", ImageFormat.Png);
                        } else
                        {
                            image.Save(directory + separator + "custom" + (i - 8) + ".png", ImageFormat.Png);
                        }
                    } else
                    {
                        image.Save(directory + separator + "custom" + i + ".png", ImageFormat.Png);
                    }
                }
                else
                {
                    if (cfg.defaultPaintings)
                    {
                        if (i < 9)
                        {
                            mls.LogWarning("Error downloading default image " + i);
                        }
                        else
                        {
                            mls.LogWarning("Error downloading custom image " + (i - 8));
                        }
                    }
                    else
                    {
                        mls.LogWarning("Error downloading custom image " + i);
                    }
                }
                i++;
            }
        }

        private void DeleteDefaultTextures(String directory)
        {
            if (File.Exists(directory + separator + "default1.png"))
            {
                for (int i = 1; i < 9; i++)
                {
                    File.Delete(directory + separator + "default" + i + ".png");
                }
            }
        }
    }
}
