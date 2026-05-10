using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;

namespace MaliMapModules
{
    internal static class EnemiesModule
    {
        private static readonly HashSet<MapCustomEntity> Markers = [];
        private static readonly HashSet<EnemyParent> Pending = [];

        private static Sprite? _enemySpriteCached;
        private static Color? _enemyColorCached;

        private static bool _runSpriteCaching = true;
        private static readonly string EnemySpriteFileName = "EnemySprite.png";

        internal static void Tick()
        {
            if (!ModuleUtils.MapReady) return;

            // Evaluate pending markers
            Pending.RemoveWhere(e => e == null || TryCreateEnemyMarker(e));
        }

        internal static void Reset()
        {
            Markers.Clear();
            Pending.Clear();
            CacheEnemySprite();
            _enemyColorCached ??= ModuleUtils.ParseColor(MaliMapModules.EnemyColorHex.Value);
        }

        private static bool TryCreateEnemyMarker(EnemyParent ep)
        {
            if (ep == null || ep.Enemy == null) return false;

            // Ensure we have a sprite
            if (_enemySpriteCached == null) return false;

            // Find and add MapCustom to either Rigidbody or GameObject
            var go = MaliMapModules.UseEnemyRigidbody.Value && ep.Enemy.HasRigidbody
                ? ep.Enemy.Rigidbody.gameObject
                : ep.Enemy.gameObject;

            var mce = ModuleUtils.TryCreateMarker(go, _enemySpriteCached, _enemyColorCached ?? Color.red);

            // If still null, something went critically wrong
            if (mce == null)
            {
                MaliMapModules.Logger.LogWarning($"[Enemies] Critical failure trying to add {ep.enemyName} map marker. Discontinuing attempts.");
                return true;
            }

            ModuleUtils.RegisterMarkerIfMine(Markers, mce, ep.enemyName, MaliMapModules.ShowEnemies.Value);
            return true;
        }

        internal static void UpdateAllMarkers()
        {
            foreach (var m in Markers)
            {
                ModuleUtils.SetMarkerVisibility(m, MaliMapModules.ShowEnemies.Value);
            }
        }

        private static void CacheEnemySprite()
        {
            if (_enemySpriteCached != null && !_runSpriteCaching) return;
            _enemySpriteCached = ModuleUtils.GetSprite(EnemySpriteFileName);
            _runSpriteCaching = false;
        }

        // Discover enemies as they are created
        [HarmonyPatch(typeof(EnemyParent), nameof(EnemyParent.Awake))]
        private static class EnemyParent_Awake_Patch
        {
            private static void Postfix(EnemyParent __instance)
            {
                if (__instance != null)
                    Pending.Add(__instance);
            }
        }

        // On spawn ensure marker exists (and is visible if toggle on)
        [HarmonyPatch(typeof(EnemyParent), nameof(EnemyParent.SpawnRPC))]
        private static class EnemyParent_SpawnRPC_Patch
        {
            private static void Postfix(EnemyParent __instance)
            {
                // If marker already exists, show
                var ent = GetMarker(__instance);
                if (ent != null)
                    ModuleUtils.SetMarkerVisibility(ent, MaliMapModules.ShowEnemies.Value);
                else
                    Pending.Add(__instance);
            }
        }

        // On despawn, hide marker, don't destroy
        [HarmonyPatch(typeof(EnemyParent), nameof(EnemyParent.DespawnRPC))]
        private static class EnemyParent_DespawnRPC_Patch
        {
            private static void Postfix(EnemyParent __instance)
            {
                var ent = GetMarker(__instance);
                if (ent != null)
                    ent.spriteRenderer.enabled = false;
            }
        }

        private static MapCustomEntity? GetMarker(EnemyParent ep)
        {
            if (ep == null || ep.Enemy == null) return null;
            var go = MaliMapModules.UseEnemyRigidbody.Value && ep.Enemy.HasRigidbody ?
                ep.Enemy.Rigidbody.gameObject : ep.Enemy.gameObject;
            var mc = go.GetComponent<MapCustom>();
            return mc?.mapCustomEntity;
        }
    }    
}