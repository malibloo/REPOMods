using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace MaliMapMods
{
    //Woe be the person who is going through this code, realizing I had to find a different way for each different part.

    [HarmonyPatch(typeof(Map), "Awake")]
    public class MapAwakePatch
    {
        static void Prefix()
        {
            Plugin.Logger.LogInfo("Reawakened map");
            //Stuff here to reset/empty the sprite list
            Plugin.recolorSprites.Clear();
            Plugin.mapLight = null;
        }
    }

    //Unexplored map (from collection on Map)
    [HarmonyPatch(typeof(Map), "Start")]
    public class MapCommonPatch
    {
        static void Postfix(ref Map __instance)
        {
            var map = __instance;
            var i = 0;
            //Regular floor
            Plugin.RegisterMaterial(Plugin.rFloor, map?.FloorObject1x1, Plugin.configFloorColor, i + 1, "Could not find floors to color!");
            //Regular wall
            Plugin.RegisterMaterial(Plugin.rWall, map?.Wall1x1Object, Plugin.configWallColor, i + 2, "Could not find walls to color!");
            //Stairs
            Plugin.RegisterMaterial(Plugin.rStairs, map?.StairsObject, Plugin.configWallColor, i + 3, "Could not find stairs to color!");
            //Truck floor
            Plugin.RegisterMaterial(Plugin.rTruckFloor, map?.FloorTruck, Plugin.configTruckFloorColor, i + 11, "Could not change truck/extraction floor color!");
            //Truck wall
            Plugin.RegisterMaterial(Plugin.rTruckWall, map?.WallTruck, Plugin.configTruckWallColor, i + 12, "Could not change truck/extraction wall color!");
            //Inactive extraction floor
            Plugin.RegisterMaterial(Plugin.rInactiveFloor, map?.FloorInactive, Plugin.configInactiveFloorColor, i + 13, "Could not change inactive extraction floor color!");
            //Inactive extraction wall
            Plugin.RegisterMaterial(Plugin.rInactiveWall, map?.WallInactive, Plugin.configInactiveWallColor, i + 14, "Could not change inactive extraction wall color!");
            //Unexplored outline
            Plugin.RegisterMaterial(Plugin.rUnexOutline, map?.RoomVolumeOutline, Plugin.configUnexOutlineColor, i + 15, "Could not change unexplored outline color!");
            //Unexplored question mark
            Plugin.RegisterSprite(Plugin.sUnexQuestion, map?.ModulePrefab, Plugin.configUnexQuestionColor, i + 16, "Could not change unexplored question mark color!");
        }
    }

    //Door Color
    [HarmonyPatch(typeof(DirtFinderMapDoor), "Start")]
    public class MapDoorPatch
    {
        static bool hasRun;
        static void Postfix(DirtFinderMapDoor __instance)
        {
            if (hasRun) return;
            hasRun = true;
            Plugin.Logger.LogInfo($"Loaded {__instance.DoorPrefab.name}");
            Plugin.RegisterMaterial(Plugin.rDoor, __instance.DoorPrefab, Plugin.configDoorColor, 20, "Could not change door color!");
        }
    }

    //Valuable color (sprite)
    [HarmonyPatch(typeof(MapValuable), "Start")]
    public class MapValuableStartPatch
    {
        static void Prefix(MapValuable __instance)
        {
            if (__instance == null) return;
            Plugin.RegisterSprite(Plugin.sValuables, __instance.gameObject, Plugin.configValuableColor, 21, "Could not change valuable color!");
        }
    }

    //Custom object color (script variable, check if refreshes)
    //This method also gets color passed along, could possibly be used for many different objects
    [HarmonyPatch(typeof(Map), "AddCustom")]
    public class MapCustomStartPatch
    {
        static void Postfix(MapCustom mapCustom)
        {
            if (mapCustom == null) return;
            Plugin.RegisterSprite(Plugin.sCustom, mapCustom.mapCustomEntity.gameObject, Plugin.configCustomColor, 29, "Could not change custom item color!");
        }
    }

    //Background, Scanlines, Player (sprite)
    [HarmonyPatch(typeof(DirtFinderMapPlayer), "Awake")]
    public class MapPlayerAwakePatch
    {
        static void Postfix(ref DirtFinderMapPlayer __instance)
        {
            //Background
            // Map/Active/Player/Background.meshRenderer
            var bgObject = __instance.transform.Find("Background")?.gameObject;
            Plugin.RegisterMaterial(Plugin.rBackground, bgObject, Plugin.configBackgroundColor, 21, "Could not change background color!");

            //Scanlines
            // Map/Active/Player/Scanlines > children.meshRenderer
            var scanLinesObject = __instance.transform.Find("Scanlines")?.gameObject;
            Plugin.RegisterMaterial(Plugin.rScanlines, scanLinesObject, Plugin.configScanlinesColor, 22, "Could not change scanlines color!");

            //Player
            // Map/Active/Player/PlayerGraphic
            var playerGraphic = __instance.transform.Find("Player Graphic")?.gameObject;
            Plugin.RegisterSprite(Plugin.sPlayer, playerGraphic, Plugin.configPlayerColor, 23, "Could not change player color!");
        }
    }

    //Light color
    [HarmonyPatch(typeof(MapToolController), "Start")]
    public class MapLightStartPatch
    {
        static void Prefix(MapToolController __instance)
        {
            if (Plugin.mapLight != null) return;
            Plugin.mapLight = __instance?.transform.parent?.GetComponentInChildren<Light>();
            if (Plugin.mapLight != null)
                Plugin.mapLight.color = Plugin.configLightColor.Value;
            else if (!Plugin.dirtyWarningCheck[30])
            {
                Plugin.Logger.LogWarning("Could not change light color!");
                Plugin.dirtyWarningCheck[30] = true;
            }
        }
    }

    //Backtrack points
    [HarmonyPatch(typeof(MapBacktrackPoint), "Awake")]
    public class MapBacktrackAwakePatch
    {
        static void Postfix(ref MapBacktrackPoint __instance)
        {
            Plugin.RegisterSprite(Plugin.sBacktrack, __instance.gameObject, Plugin.configBacktrackColor, 24, "Could not change backtrack point color!");
        }
    }
}
