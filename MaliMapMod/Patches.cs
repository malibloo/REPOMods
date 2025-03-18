using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace MaliMapMods
{
    //Woe be the person who is going through this code, realizing I had to find a different way for each different part.

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
            Plugin.RecolorSprite(map?.ModulePrefab, Plugin.configUnexQuestionColor, i + 16, "Could not change unexplored question mark color!");
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

    //Valuable color
    [HarmonyPatch(typeof(MapValuable), "Start")]
    public class MapValuableStartPatch
    {
        static void Prefix(MapValuable __instance)
        {
            if (__instance != null)
                __instance.spriteRenderer.color = Plugin.configValuableColor.Value;
            else if (!Plugin.dirtyWarningCheck[21])
            {
                Plugin.Logger.LogWarning("Could not change valuable color!");
                Plugin.dirtyWarningCheck[21] = true;
            }
        }
    }

    //Custom object color
    //This method also gets color passed along, could possibly be used for many different object
    [HarmonyPatch(typeof(MapCustom), "Start")]
    public class MapCustomStartPatch
    {
        static void Prefix(MapCustom __instance)
        {
            if (__instance != null)
                __instance.color = Plugin.configCustomColor.Value;
            else if (!Plugin.dirtyWarningCheck[29])
            {
                Plugin.Logger.LogWarning("Could not change custom item color!");
                Plugin.dirtyWarningCheck[29] = true;
            }
        }
    }

    //Background, Scanlines, Player
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
            Plugin.RecolorSprite(playerGraphic, Plugin.configPlayerColor, 23, "Could not change player color!");
        }
    }

    //Light color
    [HarmonyPatch(typeof(MapToolController), "Start")]
    public class MapLightStartPatch
    {
        static void Prefix(MapToolController __instance)
        {
            var light = __instance?.transform.parent?.GetComponentInChildren<Light>();
            if (light != null)
                light.color = Plugin.configLightColor.Value;

            else if (!Plugin.dirtyWarningCheck[30])
            {
                Plugin.Logger.LogWarning("Could not change light color!");
                Plugin.dirtyWarningCheck[30] = true;
            }
        }
    }
}
