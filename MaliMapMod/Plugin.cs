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
        //MapBacktrack
        // MapBacktrack.points spriteRenderer
        //Player Counter
        // ???


        static internal SpritesWithColor sPlayer = null;
        static internal SpritesWithColor sValuables = null;
        static internal SpritesWithColor sCustom = null;
        static internal SharedMaterialWithColor rDoor = null;
        static internal SharedMaterialWithColor rFloor = null;
        static internal SharedMaterialWithColor rWall = null;
        static internal SharedMaterialWithColor rStairs = null;
        static internal SharedMaterialWithColor rUnexOutline = null;
        static internal SpritesWithColor sUnexQuestion = null;
        static internal SharedMaterialWithColor rTruckFloor = null;
        static internal SharedMaterialWithColor rTruckWall = null;
        static internal SharedMaterialWithColor rInactiveFloor = null;
        static internal SharedMaterialWithColor rInactiveWall = null;
        static internal SharedMaterialWithColor rScanlines = null;
        static internal SharedMaterialWithColor rBackground = null;

        static internal List<SharedMaterialWithColor> recolorMaterials = [];
        static internal List<SpritesWithColor> recolorSprites = [];
        static internal Light mapLight = null;

        internal static new ManualLogSource Logger;

        static public bool[] dirtyWarningCheck = new bool[30];

        private void Awake()
        {
            // Plugin startup logic
            Logger = base.Logger;

            RegisterConfig(ref configLightColor, Config.Bind("General", "Light Color", new Color(0f, 0.0973f, 1f), "The color of the light emitting from the map.")); // 0f, 1f, 0.0973f
            RegisterConfig(ref configPlayerColor, Config.Bind("General", "Player Color", new Color(0.4481f, 0.4745f, 1f), "The color of the player on the map.")); // 0.4481f, 1f, 0.4745f
            RegisterConfig(ref configValuableColor, Config.Bind("General", "Valuable Color", new Color(1f, 0.8291f, 0f), "The color of valuables on the map.")); // 1f, 0.8291f, 0f
            RegisterConfig(ref configCustomColor, Config.Bind("General", "Custom Color", new Color(0f, 1f, 0.92f), "The color of the other items on the map, like carts, usable items.")); // 0f, 1f, 0.92f
            RegisterConfig(ref configDoorColor, Config.Bind("General", "Door Color", new Color(0f, 0f, 0f), "The color of doors on the map.")); // 0.2689 1 0.3142 1
            RegisterConfig(ref configFloorColor, Config.Bind("General.Rooms", "Floor Color", new Color(0f, 0.0115f, 0.2824f), "The color of regular floors on the map.")); // 0f 0.2824f 0.0115f
            RegisterConfig(ref configWallColor, Config.Bind("General.Rooms", "Wall Color", new Color(0f, 0.0265f, 0.4627f), "The color of regular walls on the map.")); // 0.0265f 0.4627f 0f
            RegisterConfig(ref configStairsColor, Config.Bind("General.Rooms", "Stairs Color", new Color(0.1651f, 0.2149f, 1f), "The color of stairs on the map.")); // 0.1651f, 1f, 0.2149f
            RegisterConfig(ref configTruckFloorColor, Config.Bind("General.Rooms", "Spawn & Active Extraction Floor Color", new Color(0f, 0.3182f, 0.3302f), "The color of the truck spawn and active extraction point floors on the map.")); // 0f, 0.3182f, 0.3302f
            RegisterConfig(ref configTruckWallColor, Config.Bind("General.Rooms", "Spawn & Active Extraction Wall Color", new Color(0f, 0.4757f, 0.4906f), "The color of the truck spawn and active extraction point walls on the map.")); // 0f, 0.4757f, 0.4906f
            RegisterConfig(ref configInactiveFloorColor, Config.Bind("General.Rooms", "Extraction Floor Color", new Color(0.3302f, 0f, 0.1426f), "The color of inactive extraction point floors on the map.")); // 0.3302f, 0.1426f, 0f
            RegisterConfig(ref configInactiveWallColor, Config.Bind("General.Rooms", "Extraction Wall Color", new Color(0.5566f, 0f, 0.2504f), "The color of inactive extraction points walls on the map.")); // 0.5566f, 0.2504f, 0f
            RegisterConfig(ref configUnexOutlineColor, Config.Bind("General.Rooms.Unexplored", "Unexplored Outline", new Color(0f, 0.0304f, 0.5377f), "The color of the outlines of the unexplored map.")); // 0f, 0.5377f, 0.0304f
            RegisterConfig(ref configUnexQuestionColor, Config.Bind("General.Rooms.Unexplored", "Unexplored Question Mark", new Color(0f, 0.0362f, 0.7358f), "The color of the question mark on the unexplored map.")); // 0f, 0.7358f, 0.0362f
            RegisterConfig(ref configScanlinesColor, Config.Bind("General.Background", "Scanlines Color", new Color(0.0406f, 0f, 1f, 0.0196f), "The color of the scanlines scrolling over the map. Transparency is used")); // 0.0406f, 1f, 0f, 0.0196f
            RegisterConfig(ref configBackgroundColor, Config.Bind("General.Background", "Background Color", new Color(0f, 0.0068f, 0.3113f), "The color the background of the map.")); // 0f 0.3113f 0.0068f

            //Recolor dead heads

            var harmony = new Harmony(MyPluginInfo.PLUGIN_GUID);
            harmony.PatchAll();

            Logger.LogInfo($"Color Patch Mod loaded!");
        }

        static void RegisterConfig<T>(ref ConfigEntry<T> staticConfig, ConfigEntry<T> newConfig)
        {
            staticConfig = newConfig;
            staticConfig.SettingChanged += OnConfigChanged;
            //Logger.LogInfo($"Added config: {staticConfig.Value} {staticConfig == null}:{newConfig == null} ");
        }

        static public void OnConfigChanged(object sender, EventArgs e)
        {
            if (sender is ConfigEntry<Color> c)
            {
                var mc = recolorMaterials.FirstOrDefault(r => r.ConfigEntry == c);
                if (mc != null)
                {
                    RecolorMaterial(mc);
                    return;
                }
                var sc = recolorSprites.FirstOrDefault(r => r.ConfigEntry == c);
                if (sc != null)
                {
                    RecolorSprites(sc);
                    return;
                }
                if (c == configLightColor && mapLight != null)
                    mapLight.color = c.Value;
            }
        }


        static internal void RegisterMaterial(SharedMaterialWithColor mc, GameObject o, ConfigEntry<Color> ce, int wId, string warning = "")
        {
            if (o == null || mc != null)
                return; //Object not found or already exists, exit
            var r = o.GetComponentInChildren<Renderer>() ?? o.GetComponent<Renderer>();
            mc = new(r?.sharedMaterial, ce, wId, warning);
            recolorMaterials.Add(mc);
            RecolorMaterial(mc);
        }

        internal static void RegisterSprite(SpritesWithColor sc, GameObject o, ConfigEntry<Color> ce, int wId, string warning = "")
        {
            if (o == null) return;
            if (sc == null)
            {
                sc = new([], ce, wId, warning);
                recolorSprites.Add(sc);
            }
            var spriteRenderer = o.GetComponentInChildren<SpriteRenderer>() ?? o.GetComponent<SpriteRenderer>();
            if (sc.SpriteRenderers.Contains(spriteRenderer))
                return;
            sc.SpriteRenderers.Add(spriteRenderer);

            spriteRenderer.color = sc.ConfigEntry.Value;
        }

        static internal void RecolorMaterial(SharedMaterialWithColor rc)
        {
            if (rc?.SharedMaterial != null)
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

        static internal void RecolorSprites(SpritesWithColor sc)
        {
            if (sc.SpriteRenderers == null || sc.SpriteRenderers.Count == 0)
                return;
            bool success = true;
            Color oc = new();
            foreach (var sr in sc.SpriteRenderers)
            {
                if (sr == null)
                {
                    success = false;
                    continue;
                }
                oc = sr.color;
                sr.color = sc.ConfigEntry.Value;
            }
            if (success)
                Logger.LogInfo($"Recolored <{sc.SpriteRenderers[0].gameObject.name}> from ({ColorUtility.ToHtmlStringRGBA(oc)}) to ({ColorUtility.ToHtmlStringRGBA(sc.ConfigEntry.Value)})");
            else if (dirtyWarningCheck[sc.WarningId])
            {
                Logger.LogWarning(sc.WarningText);
                dirtyWarningCheck[sc.WarningId] = true;
            }
        }
    }

    public class SharedMaterialWithColor(Material material, ConfigEntry<Color> configEntry, int warningId, string warningText)
    {
        public Material SharedMaterial { get; set; } = material;
        public ConfigEntry<Color> ConfigEntry { get; set; } = configEntry;
        public int WarningId { get; set; } = warningId;
        public string WarningText { get; set; } = warningText;
    }

    public class SpritesWithColor(List<SpriteRenderer> sprites, ConfigEntry<Color> configEntry, int warningId, string warningText)
    {
        public List<SpriteRenderer> SpriteRenderers { get; set; } = sprites;
        public ConfigEntry<Color> ConfigEntry { get; set; } = configEntry;
        public int WarningId { get; set; } = warningId;
        public string WarningText { get; set; } = warningText;
    }

    //Maphack (Map > Map Controller > Over Layer (just disable this layer))
}
