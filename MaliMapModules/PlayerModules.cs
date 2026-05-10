using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;

namespace MaliMapModules
{
    internal static class PlayerModules
    {
        private static readonly HashSet<MapCustomEntity> Markers = [];
        private static readonly HashSet<PlayerAvatar> Pending = [];

        private static Sprite? _playerSpriteCached;
        private static Color? _playerColorConfig;

        private static bool _runSpriteCaching = true;
        private static readonly string PlayerSpriteFileName = "PlayerSprite.png";

        internal static void Tick()
        {
            if (!ModuleUtils.MapReady) return;

            // Evaluate pending markers
            Pending.RemoveWhere(p => p == null || TryCreatePlayerMarker(p));
        }

        internal static void Reset()
        {
            Markers.Clear();
            Pending.Clear();
            CachePlayerSprite();
            _playerColorConfig ??= ModuleUtils.ParseColor(MaliMapModules.PlayerColorOverrideHex.Value);
        }

        private static bool TryCreatePlayerMarker(PlayerAvatar pa)
        {
            if (pa == null) return false;
            if (pa.isLocal) return true; // Don't create marker for local player

            // Ensure we have a sprite
            if (_playerSpriteCached == null) return false;
            // Get color, either from player visuals, config, or default to white
            // First try index color (5 is top of head, 6 is bottom of head)
            Color color;
            if (_playerColorConfig != null)
                color = _playerColorConfig.Value;
            else if (pa.playerCosmetics?.colorsEquipped != null && pa.playerCosmetics.colorsEquipped.Length > 5)
                color = MetaManager.instance.colors[pa.playerCosmetics.colorsEquipped[5]].color;
            else
                color = Color.white;

            var go = TryGetRigidBodyGO(pa.gameObject);

            var mce = ModuleUtils.TryCreateMarker(go, _playerSpriteCached, color);

            // If still null, something went critically wrong
            if (mce == null)
            {
                MaliMapModules.Logger.LogWarning($"[Players] Critical failure trying to add {pa.playerName} map marker. Discontinuing attempts.");
                return true;
            }

            ModuleUtils.RegisterMarkerIfMine(Markers, mce, pa.playerName, MaliMapModules.ShowPlayers.Value);
            return true;
        }

        private static GameObject TryGetRigidBodyGO(GameObject go)
        {
            // TODO: Find better reference to rigidbody, check game's hierarchy
            return go.GetComponentInChildren<Rigidbody>()?.gameObject ?? go;
        }

        internal static void UpdateAllMarkers()
        {
            foreach (var m in Markers)
            {
                ModuleUtils.SetMarkerVisibility(m, MaliMapModules.ShowPlayers.Value);
            }
        }

        private static void CachePlayerSprite()
        {
            if (_playerSpriteCached != null && !_runSpriteCaching) return;
            _playerSpriteCached = ModuleUtils.GetSprite(PlayerSpriteFileName);
            _runSpriteCaching = false;
        }

        // Discover players are they are created, add to pending
        [HarmonyPatch(typeof(PlayerAvatar), nameof(PlayerAvatar.Awake))]
        private static class PlayerAvatar_Awake_Patch
        {
            private static void Postfix(PlayerAvatar __instance)
            {
                if (__instance != null)
                    Pending.Add(__instance);
            }
        }

        // Player revive, show marker again
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
                        ModuleUtils.SetMarkerVisibility(m, MaliMapModules.ShowPlayers.Value);
                        break;
                    }
                }
            }
        }

        // Player death, hide marker
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
    }
}