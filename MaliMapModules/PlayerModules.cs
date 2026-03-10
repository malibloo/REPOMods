using HarmonyLib;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace MaliMapModules
{
    partial class MaliMapModules
    {
        internal class PlayerModules
        {
            private static readonly HashSet<MapCustomEntity> Markers = [];
            private static readonly HashSet<PlayerAvatar> Pending = [];


            private static Sprite? _playerSpriteCached;
            private static Color? _playerColorCached;

            private static bool _spriteCacheAttempted = false;


            internal static void Tick()
            {
                if (!MapReady) return;
                // Escape if no sprites were found
                if(_spriteCacheAttempted && _playerSpriteCached == null) return;

                // Cleanup
                Markers.RemoveWhere(m => m == null);
                Pending.RemoveWhere(v => v == null);

                CachePlayerSprite();
                _playerColorCached ??= ParseColor(PlayerColorHex.Value);

                // Evaluate pending markers
                Pending.RemoveWhere(p => p != null && TryCreatePlayerMarker(p));
            }

            private static bool TryCreatePlayerMarker(PlayerAvatar pa)
            {
                if (pa == null) return false;

                // Ensure we have a sprite
                var sprite = _playerSpriteCached;
                if (sprite == null) return false;

                // Try player rigidbody first

                GameObject go = TryGetRigidBodyGO(pa.gameObject);

                var mc = go.GetComponent<MapCustom>();
                if (mc != null) return true;

                mc = go.AddComponent<MapCustom>();
                mc.sprite = sprite;
                mc.color = _playerColorCached ?? Color.cyan;
                mc.enabled = false; // Disable to prevent Start() to avoid duplicates

                Map.Instance.AddCustom(mc, sprite, mc.color);

                // If still null, something went critically wrong
                if (mc.mapCustomEntity == null)
                {
                    Logger.LogWarning($"[Enemies] Critical failure trying to add {pa.name} map marker. Discontinuing attempts.");
                    return true;
                }

                RegisterMarkerIfMine(mc.mapCustomEntity, pa.playerName);
                return true;
            }

            private static GameObject TryGetRigidBodyGO(GameObject go)
            {
                // TODO: Find better reference to rigidbody, check game's hierarchy
                return go.GetComponentInChildren<Rigidbody>()?.gameObject ?? go;
            }

            private static void RegisterMarkerIfMine(MapCustomEntity ent, string name)
            {
                if (ent == null) return;
                Markers.Add(ent);
                ent.gameObject.name = $"[MMM] Marker - {name}";
                SetMarkerVisibility(ent);
            }

            private static void SetMarkerVisibility(MapCustomEntity m)
            {
                if (m == null || m.spriteRenderer == null) return;

                bool alive = m.Parent != null && m.Parent.gameObject.activeInHierarchy;

                m.spriteRenderer.enabled = ShowPlayers.Value && alive;
            }

            internal static void UpdateAllMarkers()
            {
                foreach (var m in Markers)
                {
                    SetMarkerVisibility(m);
                }
            }

            private static void CachePlayerSprite(bool forceRefresh = false)
            {
                if (_playerSpriteCached != null && !forceRefresh) return;
                // Load in sprite from local file if it exists, otherwise fall back to the default sprite from the game assets.
                var customSprite = GetSprite("PlayerSprite.png");
                if (customSprite != null)
                {
                    _playerSpriteCached = customSprite;
                    return;
                }
                if (Map.Instance.ValuableObject != null)
                {
                    var mv = Map.Instance.ValuableObject.GetComponent<MapValuable>();
                    if (mv != null)
                    {
                        _playerSpriteCached = mv.spriteBig;
                    }
                }
                _spriteCacheAttempted = true;
            }

            private static Sprite? GetSprite(string filePath)
            {
                var path = Path.Combine(Path.GetDirectoryName(Instance.Info.Location) ?? "", filePath);
                if (!File.Exists(path))
                {
                    Logger.LogInfo($"Sprite ({filePath}) not found, using default sprite.");
                    return null;
                }
                var tex = new Texture2D(2, 2);
                ImageConversion.LoadImage(tex, File.ReadAllBytes(path));
                return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
            }

            [HarmonyPatch(typeof(PlayerAvatar), nameof(PlayerAvatar.Awake))]
            private static class PlayerAvatar_Awake_Patch
            {
                private static void Postfix(PlayerAvatar __instance)
                {
                    if (__instance == null) return;
                    Pending.Add(__instance);
                }
            }

            [HarmonyPatch(typeof(PlayerAvatar), "PlayerDeathDone")]
            private static class PlayerAvatar_PlayerDeathDone_Patch
            {
                private static void Postfix(PlayerAvatar __instance)
                {
                    if (__instance == null || Markers.Count == 0) return;
                    var go = TryGetRigidBodyGO(__instance.gameObject);
                    // Update marker visibility on death
                    foreach (var m in Markers)
                    {
                        if (m.Parent.gameObject == go)
                        {
                            m.spriteRenderer.enabled = false;
                            break;
                        }
                    }
                }
            }

            [HarmonyPatch(typeof(PlayerAvatar), nameof(PlayerAvatar.ReviveRPC))]
            private static class PlayerAvatar_ReviveRPC_Patch
            {
                private static void Postfix(PlayerAvatar __instance)
                {
                    if (__instance == null || Markers.Count == 0) return;
                    var go = TryGetRigidBodyGO(__instance.gameObject);
                    // Update marker visibility on revive
                    foreach (var m in Markers)
                    {
                        if (m.Parent.gameObject == go)
                        {
                            SetMarkerVisibility(m);
                            break;
                        }
                    }
                }
            }
        }
    }
}