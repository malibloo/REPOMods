using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Xml.Linq;
using UnityEngine;
using UnityEngine.UIElements.UIR;

namespace MaliMapMods
{
    [BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
    [BepInProcess("REPO.exe")]
    public class Plugin : BaseUnityPlugin
    {
        static public ConfigEntry<Color> configLightColor; //Light
        static public ConfigEntry<Color> configPlayerColor; //Sprite
        static public ConfigEntry<Color> configValuableColor; //Sprite
        static public ConfigEntry<Color> configCustomColor; //Sprite
        static public ConfigEntry<Color> configDoorColor;
        static public ConfigEntry<Color> configFloorColor;
        static public ConfigEntry<Color> configWallColor;
        static public ConfigEntry<Color> configStairsColor;
        static public ConfigEntry<Color> configUnexOutlineColor;
        static public ConfigEntry<Color> configUnexQuestionColor;
        static public ConfigEntry<Color> configTruckFloorColor;
        static public ConfigEntry<Color> configTruckWallColor;
        static public ConfigEntry<Color> configInactiveFloorColor;
        static public ConfigEntry<Color> configInactiveWallColor;
        static public ConfigEntry<Color> configScanlinesColor;
        static public ConfigEntry<Color> configBackgroundColor;

        static internal SharedMaterialWithColor rDoor = null;
        static internal SharedMaterialWithColor rFloor = null;
        static internal SharedMaterialWithColor rWall = null;
        static internal SharedMaterialWithColor rStairs = null;
        static internal SharedMaterialWithColor rUnexOutline = null;
        static internal SharedMaterialWithColor rUnexQuestion = null;
        static internal SharedMaterialWithColor rTruckFloor = null;
        static internal SharedMaterialWithColor rTruckWall = null;
        static internal SharedMaterialWithColor rInactiveFloor = null;
        static internal SharedMaterialWithColor rInactiveWall = null;
        static internal SharedMaterialWithColor rScanlines = null;
        static internal SharedMaterialWithColor rBackground = null;

        static internal List<SharedMaterialWithColor> recolorMaterials = new();
        static internal List<SharedMaterialWithColor> recolorSprites = new();

        internal static new ManualLogSource Logger;

        static public bool[] dirtyWarningCheck = new bool[30];

        private void Awake()
        {
            // Plugin startup logic
            Logger = base.Logger;

            RegisterConfig(configLightColor, Config.Bind("General", "Light Color", new Color(0f, 0.0973f, 1f), "The color of the light emitting from the map.")); // 0f, 1f, 0.0973f
            RegisterConfig(configPlayerColor, Config.Bind("General", "Player Color", new Color(0.4481f, 0.4745f, 1f), "The color of the player on the map.")); // 0.4481f, 1f, 0.4745f
            RegisterConfig(configValuableColor, Config.Bind("General", "Valuable Color", new Color(1f, 0.8291f, 0f), "The color of valuables on the map.")); // 1f, 0.8291f, 0f
            RegisterConfig(configCustomColor, Config.Bind("General", "Custom Color", new Color(0f, 1f, 0.92f), "The color of the other items on the map, like carts, usable items.")); // 0f, 1f, 0.92f
            RegisterConfig(configDoorColor, Config.Bind("General", "Door Color", new Color(0f, 0f, 0f), "The color of doors on the map.")); // 0.2689 1 0.3142 1
            RegisterConfig(configFloorColor, Config.Bind("General.Rooms", "Floor Color", new Color(0f, 0.0115f, 0.2824f), "The color of regular floors on the map.")); // 0f 0.2824f 0.0115f
            RegisterConfig(configWallColor, Config.Bind("General.Rooms", "Wall Color", new Color(0f, 0.0265f, 0.4627f), "The color of regular walls on the map.")); // 0.0265f 0.4627f 0f
            RegisterConfig(configStairsColor, Config.Bind("General.Rooms", "Stairs Color", new Color(0.1651f, 0.2149f, 1f), "The color of stairs on the map.")); // 0.1651f, 1f, 0.2149f
            RegisterConfig(configTruckFloorColor, Config.Bind("General.Rooms", "Spawn & Active Extraction Floor Color", new Color(0f, 0.3182f, 0.3302f), "The color of the truck spawn and active extraction point floors on the map.")); // 0f, 0.3182f, 0.3302f
            RegisterConfig(configTruckWallColor, Config.Bind("General.Rooms", "Spawn & Active Extraction Wall Color", new Color(0f, 0.4757f, 0.4906f), "The color of the truck spawn and active extraction point walls on the map.")); // 0f, 0.4757f, 0.4906f
            RegisterConfig(configInactiveFloorColor, Config.Bind("General.Rooms", "Extraction Floor Color", new Color(0.3302f, 0f, 0.1426f), "The color of inactive extraction point floors on the map.")); // 0.3302f, 0.1426f, 0f
            RegisterConfig(configInactiveWallColor, Config.Bind("General.Rooms", "Extraction Wall Color", new Color(0.5566f, 0f, 0.2504f), "The color of inactive extraction points walls on the map.")); // 0.5566f, 0.2504f, 0f
            RegisterConfig(configUnexOutlineColor, Config.Bind("General.Rooms.Unexplored", "Unexplored Outline", new Color(0f, 0.0304f, 0.5377f), "The color of the outlines of the unexplored map.")); // 0f, 0.5377f, 0.0304f
            RegisterConfig(configUnexQuestionColor, Config.Bind("General.Rooms.Unexplored", "Unexplored Question Mark", new Color(0f, 0.0362f, 0.7358f), "The color of the question mark on the unexplored map.")); // 0f, 0.7358f, 0.0362f
            RegisterConfig(configScanlinesColor, Config.Bind("General.Background", "Scanlines Color", new Color(0.0406f, 0f, 1f, 0.0196f), "The color of the scanlines scrolling over the map. Transparency is used")); // 0.0406f, 1f, 0f, 0.0196f
            RegisterConfig(configBackgroundColor, Config.Bind("General.Background", "Background Color", new Color(0f, 0.0068f, 0.3113f), "The color the background of the map.")); // 0f 0.3113f 0.0068f

            //Recolor dead heads

            var harmony = new Harmony(MyPluginInfo.PLUGIN_GUID);
            harmony.PatchAll();

            Logger.LogInfo($"Color Patch Mod loaded!");
        }

        static void RegisterConfig<T>(ConfigEntry<T> staticConfig, ConfigEntry<T> newConfig)
        {
            staticConfig = newConfig;
            staticConfig.SettingChanged += OnConfigChanged;
        }

        static public void OnConfigChanged(object sender, EventArgs e)
        {
            if (sender is ConfigEntry<Color> c)
            {
                var rc = recolorMaterials.FirstOrDefault(r => r.ConfigEntry == c);
                if (rc != null)
                {
                    RecolorMaterial(rc);
                    return;
                }
                //Some stuff about recoloring sprites
            }
        }

        static internal void RegisterMaterial(SharedMaterialWithColor rc, GameObject o, ConfigEntry<Color> ce, int wId, string warning = "")
        {
            var r = o?.GetComponentInChildren<Renderer>() ?? o?.GetComponent<Renderer>();
            rc = new(r.sharedMaterial, ce, wId, warning);
            if (!recolorMaterials.Any(i => i.ConfigEntry == ce))
                recolorMaterials.Add(rc);
            RecolorMaterial(rc);
        }

        static internal void RecolorMaterial(SharedMaterialWithColor rc)
        {
            if (rc.SharedMaterial != null)
            {
                var originalColor = rc.SharedMaterial.color;
                rc.SharedMaterial.color = rc.ConfigEntry.Value;
                Logger.LogInfo($"Recolored [{rc.SharedMaterial.name}] from ({ColorUtility.ToHtmlStringRGBA(originalColor)}) to ({ColorUtility.ToHtmlStringRGBA(rc.ConfigEntry.Value)})");
            }
            else if (dirtyWarningCheck[rc.WarningId])
            {
                dirtyWarningCheck[rc.WarningId] = true;
                Logger.LogWarning(rc.WarningText);
            }
        }

        static internal void RecolorSprite(GameObject o, ConfigEntry<Color> c, int wId, string warning = "")
        {
            var r = o?.GetComponentInChildren<SpriteRenderer>() ?? o?.GetComponent<SpriteRenderer>();
            if (r != null)
            {
                var oc = r.color;
                r.color = c.Value;
                Logger.LogInfo($"Recolored <{r.name}> from ({ColorUtility.ToHtmlStringRGBA(oc)}) to ({ColorUtility.ToHtmlStringRGBA(c.Value)})");
            }
            else if (dirtyWarningCheck[wId])
            {
                Logger.LogWarning(warning);
                dirtyWarningCheck[wId] = true;
            }
        }
    }

    public class SharedMaterialWithColor
    {
        public Material SharedMaterial { get; set; }
        public ConfigEntry<Color> ConfigEntry { get; set; }
        public int WarningId { get; set; }
        public string WarningText { get; set; }
        public SharedMaterialWithColor(Material material, ConfigEntry<Color> configEntry, int warningId, string warningText)
        {
            SharedMaterial = material;
            ConfigEntry = configEntry;
            WarningId = warningId;
            WarningText = warningText;
        }
    }

    public class SpritesWithColor
    {
        public SpriteRenderer[] Sprites { get; set; }
        public ConfigEntry<Color> ConfigEntry { get; set; }
        public int WarningId { get; set; }
        public string WarningText { get; set; }
        public SpritesWithColor(SpriteRenderer[] sprites, ConfigEntry<Color> configEntry, int warningId, string warningText)
        {
            Sprites = sprites;
            ConfigEntry = configEntry;
            WarningId = warningId;
            WarningText = warningText;
        }
    }

    //Maphack (Map > Map Controller > Over Layer (just disable this layer))
}
