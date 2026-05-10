using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace MaliMapModules
{
    internal static class PlayerDeathHeadModules
    {
        private static readonly HashSet<MapCustomEntity> Markers = [];
        private static readonly HashSet<PlayerDeathHead> ActiveHeads = [];

        private static Sprite? _deathHeadSpriteCached;
        private static Color? _deathHeadColorConfig;

        private static bool _runSpriteCaching = true;
        private static readonly string DeathHeadSpriteFileName = "PlayerDeathHeadSprite.png";

        private static readonly AccessTools.FieldRef<MapCustomEntity, float> HideTimer
            = AccessTools.FieldRefAccess<MapCustomEntity, float>("mapCustomHideTimer");


        internal static void UpdateAllMarkers()
        {
            foreach (var m in Markers)
            {
                ModuleUtils.SetMarkerVisibility(m, MaliMapModules.ShowDeathHeads.Value);
            }
        }

        internal static void Reset()
        {
            Markers.Clear();
            ActiveHeads.Clear();
            CacheDeathHeadSprite();
            _deathHeadColorConfig ??= ModuleUtils.ParseColor(MaliMapModules.DeathHeadColorOverrideHex.Value);
        }

        private static void CacheDeathHeadSprite()
        {
            if (_deathHeadSpriteCached != null && !_runSpriteCaching) return;
            _deathHeadSpriteCached = ModuleUtils.GetSprite(DeathHeadSpriteFileName);
            _runSpriteCaching = false;
        }

        [HarmonyPatch(typeof(MapCustomEntity), nameof(MapCustomEntity.Hide))]
        private static class MapCustomEntity_Hide_Patch
        {
            private static bool Prefix(MapCustomEntity __instance)
            {
                var deathHead = __instance.mapCustom?.GetComponent<PlayerDeathHead>();
                if (deathHead == null || !ActiveHeads.Contains(deathHead) || !MaliMapModules.ShowDeathHeads.Value)
                    return true;

                HideTimer(__instance) = 0f;
                return false;
            }
        }

        [HarmonyPatch(typeof(PlayerDeathHead), nameof(PlayerDeathHead.Trigger))]
        private static class PlayerDeathHead_Trigger_Patch
        {
            private static void Postfix(PlayerDeathHead __instance)
                => ActiveHeads.Add(__instance);
        }

        [HarmonyPatch(typeof(PlayerDeathHead), nameof(PlayerDeathHead.Reset))]
        private static class PlayerDeathHead_Reset_Patch
        {
            private static void Postfix(PlayerDeathHead __instance)
                => ActiveHeads.Remove(__instance);
        }

    }
}
