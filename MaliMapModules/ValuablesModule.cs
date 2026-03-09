using HarmonyLib;
using System;
using System.Collections.Generic;

namespace MaliMapModules
{
    public partial class MaliMapModules
    {
        internal static class ValuablesModule
        {
            private static readonly HashSet<ValuableObject> Pending = [];
            private static readonly HashSet<MapValuable> Markers = [];

            private static bool _capturing;
            private static int _overlayChildCountBefore;

            internal static void Tick()
            {
                Markers.RemoveWhere(m => m == null);
                Pending.RemoveWhere(v => v == null);


                if (!MapReady || Map.Instance == null) return;
                if (Pending.Count == 0) return;

                // Flush, create markers locally
                _capturing = true;
                try
                {
                    foreach (var v in Pending)
                    {
                        if (v == null) continue;
                        Map.Instance.AddValuable(v); // Client only
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogError($"Valuables flush error: {ex}");
                }
                finally
                {
                    _capturing = false;
                    Pending.Clear();
                }

                UpdateAllMarkers();
            }

            internal static void SetMarkerVisibility(MapValuable m)
            {
                if (m == null || m.spriteRenderer == null) return;

                m.spriteRenderer.enabled = ShowValuables.Value;
            }

            internal static void UpdateAllMarkers()
            {
                foreach (var m in Markers)
                {
                    SetMarkerVisibility(m);
                }
            }

            // Add valuables to pending as they spawn
            [HarmonyPatch(typeof(ValuableObject), "Start")]
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
                static void Prefix(Map __instance)
                {
                    if (!_capturing) return;
                    _overlayChildCountBefore = __instance.OverLayerParent.transform.childCount;
                }

                static void Postfix(Map __instance, ValuableObject _valuable)
                {
                    if (!_capturing) return;

                    var parent = __instance.OverLayerParent.transform;
                    int after = parent.childCount;

                    // Typically exactly one new child: index == _overlayChildCountBefore
                    for (int i = _overlayChildCountBefore; i < after; i++)
                    {
                        var child = parent.GetChild(i);
                        var mv = child.GetComponent<MapValuable>();
                        if (mv != null && mv.target == _valuable)
                        {
                            Markers.Add(mv);
                            break;
                        }
                    }
                }
            }
        }
    }
}