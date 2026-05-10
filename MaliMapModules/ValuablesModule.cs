using HarmonyLib;
using System.Collections.Generic;

namespace MaliMapModules
{
    internal static class ValuablesModule
    {
        private static readonly HashSet<MapValuable> Markers = [];
        private static readonly HashSet<ValuableObject> Pending = [];
        private static readonly HashSet<ValuableObject> ModSpawned = [];


        internal static void Tick()
        {
            if (!ModuleUtils.MapReady) return;

            Markers.RemoveWhere(m => m == null);
            Pending.RemoveWhere(v => v == null);

            if (Pending.Count == 0) return;

            foreach (var v in Pending)
            {
                if (v == null) continue;
                ModSpawned.Add(v); // Mark mod spawned valuables for hiding toggle
                Map.Instance.AddValuable(v); // Client only
            }
            Pending.Clear();

            UpdateAllMarkers();
        }

        internal static void Reset()
        {
            Markers.Clear();
            Pending.Clear();
            ModSpawned.Clear();
        }

        internal static void SetMarkerVisibility(MapValuable m)
        {
            if (m == null || m.spriteRenderer == null) return;

            m.spriteRenderer.enabled = MaliMapModules.ShowValuables.Value;
        }

        internal static void UpdateAllMarkers()
        {
            foreach (var m in Markers)
            {
                SetMarkerVisibility(m);
            }
        }


        // Add valuables to pending as they spawn
        [HarmonyPatch(typeof(ValuableObject), nameof(ValuableObject.Start))]
        private static class ValuableObject_Start_Patch
        {
            private static void Postfix(ValuableObject __instance)
            {
                if (__instance == null) return;
                Pending.Add(__instance);
            }
        }

        // Capture markers as they are added to the map
        [HarmonyPatch(typeof(Map), nameof(Map.AddValuable))]
        static class Map_AddValuable_Patch
        {
            static void Postfix(Map __instance, ValuableObject _valuable)
            {
                if (ModSpawned.Remove(_valuable))
                {
                    // Mod-initiated addition
                    var parent = __instance.OverLayerParent.transform;
                    // Reverse search because newly added objects will be at the end of the child list
                    for (int i = parent.childCount - 1; i >= 0; i--)
                    {
                        var mv = parent.GetChild(i).GetComponent<MapValuable>();
                        if (mv != null && mv.target == _valuable)
                        {
                            Markers.Add(mv);
                            break;
                        }
                    }
                }
            }
        }

        // Intercept discovery to prevent duplicate map markers and ensure proper state
        [HarmonyPatch(typeof(ValuableObject), nameof(ValuableObject.DiscoverRPC))]
        private static class ValuableObject_DiscoverRPC_Patch
        {
            static bool Prefix(ValuableObject __instance)
            {
                MapValuable? existing = null;
                foreach (var m in Markers)
                {
                    if (m != null && m.target == __instance)
                    {
                        existing = m;
                        break;
                    }
                }

                if (existing != null)
                {
                    existing.spriteRenderer.enabled = true; // Ensure marker is visible, regardless of toggle state
                    Markers.Remove(existing);
                    __instance.discovered = true;
                    return false; // Skip original, which would call Map.Instance.AddValuable
                }

                return true; // No marker yet, let the original run normally
            }
        }
    }
    
}