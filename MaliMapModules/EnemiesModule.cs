using HarmonyLib;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MaliMapModules
{
    public partial class MaliMapModules
    {
        internal static class EnemiesModule
        {
            private static readonly HashSet<MapCustomEntity> Markers = [];
            private static readonly HashSet<EnemyParent> Pending = [];

            private static Sprite? _enemySpriteCached;
            private static Color? _enemyColorCached;

            internal static void Tick()
            {
                if (!MapReady) return;

                // Cleanup
                Markers.RemoveWhere(m => m == null);
                Pending.RemoveWhere(v => v == null);

                CacheEnemySprite();
                _enemyColorCached ??= ParseColor(EnemyColorHex.Value);

                // Evaluate pending markers
                Pending.RemoveWhere(ep => ep != null && TryCreateEnemyMarker(ep));
            }

            private static void SetMarkerVisibility(MapCustomEntity m)
            {
                if (m == null || m.spriteRenderer == null) return;

                bool alive = m.Parent != null && m.Parent.gameObject.activeInHierarchy;

                m.spriteRenderer.enabled = ShowEnemies.Value && alive;
            }

            internal static void UpdateAllMarkers()
            {
                foreach (var m in Markers)
                {
                    SetMarkerVisibility(m);
                }
            }

            private static void CacheEnemySprite()
            {
                if (_enemySpriteCached != null || Map.Instance.ValuableObject == null) return;

                var mv = Map.Instance.ValuableObject.GetComponent<MapValuable>();
                if (mv != null)
                {
                    _enemySpriteCached = mv.spriteBig;
                    // TODO: Implement differently sized sprites for different enemy types.
                }
            }

            private static bool TryCreateEnemyMarker(EnemyParent ep)
            {
                if (ep == null || ep.Enemy == null) return false;

                // Ensure we have a sprite
                var sprite = _enemySpriteCached;
                if (sprite == null) return false;

                // Find and add MapCustom to either Rigidbody or GameObject
                var go = UseEnemyRigidbody.Value && ep.Enemy.HasRigidbody
                    ? ep.Enemy.Rigidbody.gameObject
                    : ep.Enemy.gameObject;

                var mc = go.GetComponent<MapCustom>();
                if (mc != null) return true;

                mc = go.AddComponent<MapCustom>();
                mc.sprite = sprite;
                mc.color = _enemyColorCached ?? Color.magenta;
                mc.enabled = false; // Disable to prevent Start() to avoid duplicates

                // Manual add MapCustomEntity, as we keep Start()
                Map.Instance.AddCustom(mc, sprite, mc.color);

                // If still null, something went critically wrong
                if (mc.mapCustomEntity == null)
                {
                    Logger.LogWarning($"[Enemies] Critical failure trying to add {ep.enemyName}. Discontinuing attempts.");
                    return true;
                }

                RegisterMarkerIfMine(mc.mapCustomEntity, ep);
                return true;
            }

            private static void RegisterMarkerIfMine(MapCustomEntity ent, EnemyParent ep)
            {
                if (ent == null) return;
                Markers.Add(ent);
                ent.gameObject.name = $"[MMM] Marker - {ep.enemyName}";
                //Logger.LogInfo($"[Enemies] Registered: {ep.enemyName}. Markers: {Markers.Count}");
                SetMarkerVisibility(ent);
            }

            // Discover enemies as they appear
            [HarmonyPatch(typeof(EnemyParent), "Awake")]
            private static class Patch_EnemyParent_Awake
            {
                private static void Postfix(EnemyParent __instance)
                {
                    if (__instance != null)
                        Pending.Add(__instance);
                }
            }

            // On spawn ensure marker exists (and is visible if toggle on)
            [HarmonyPatch(typeof(EnemyParent), "SpawnRPC")]
            private static class Patch_EnemyParent_SpawnRPC
            {
                private static void Postfix(EnemyParent __instance)
                {
                    // If marker already exists, show
                    var ent = GetMarker(__instance);
                    if (ent != null)
                        SetMarkerVisibility(ent);
                    else
                        Pending.Add(__instance);
                }
            }

            [HarmonyPatch(typeof(EnemyParent), "DespawnRPC")]
            private static class EnemyParent_DespawnRPC_Patch
            {
                private static void Postfix(EnemyParent __instance)
                {
                    var ent = GetMarker(__instance);
                    if (ent != null)
                        SetMarkerVisibility(ent);
                }
            }

            private static MapCustomEntity? GetMarker(EnemyParent ep)
            {
                if (ep == null || ep.Enemy == null) return null;
                var go = UseEnemyRigidbody.Value && ep.Enemy.HasRigidbody ?
                    ep.Enemy.Rigidbody.gameObject : ep.Enemy.gameObject;
                var mc = go.GetComponent<MapCustom>();
                return mc != null ? mc.mapCustomEntity : null;
            }
        }
    }
}