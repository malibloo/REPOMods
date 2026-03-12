using BepInEx.Configuration;
using HarmonyLib;
using Photon.Realtime;
using System.Collections.Generic;
using UnityEngine;

namespace MaliTweaks
{
    internal static class ValuableProtection
    {
        internal static readonly HashSet<ValuableObject> Protected = [];

        // Empty list on spawning valuables
        //[HarmonyPatch(typeof(ValuableDirector), nameof(ValuableDirector.SetupHost))]
        //private static class ValuableDirector_SetupHost_Patch
        [HarmonyPatch(typeof(GameDirector), nameof(GameDirector.gameStateEnd))]
        private static class GameDirector_gameStateEnd_Patch
        {
            private static void Prefix()
            {
                Protected.Clear();
            }
        }

        // Protect on spawn
        [HarmonyPatch(typeof(ValuableObject), nameof(ValuableObject.Start))]
        private static class ValuableObject_Start_Patch
        {
            private static void Postfix(ValuableObject __instance)
            {
                if (__instance != null)
                    Protected.Add(__instance);
            }
        }

        // Unprotect on discovery
        [HarmonyPatch(typeof(ValuableObject), nameof(ValuableObject.DiscoverRPC))]
        private static class ValuableObject_DiscoverRPC_Patch
        {
            private static void Postfix(ValuableObject __instance)
            {
                if (Protected.Remove(__instance)) MaliTweaks.Logger.LogInfo($"[MT] Unprotected via Discovery: {__instance.name}");
            }
        }

        // Unprotect on grab
        [HarmonyPatch(typeof(PhysGrabObject), nameof(PhysGrabObject.GrabStartedRPC))]
        private static class PhysGrabObject_GrabStartedRPC_Patch
        {
            private static void Postfix(PhysGrabObject __instance)
            {
                var valuable = __instance.GetComponent<ValuableObject>();
                if (valuable != null)
                    if (Protected.Remove(valuable)) MaliTweaks.Logger.LogInfo($"[MT] Unprotected via grab: {valuable.name}");
            }
        }

        // Unprotect when hit by explosion or other HurtCollider
        [HarmonyPatch(typeof(HurtCollider), nameof(HurtCollider.PhysObjectHurt))]
        private static class HurtCollider_PhysObjectHurt_Patch
        {
            private static void Prefix(PhysGrabObject physGrabObject)
            {
                var valuable = physGrabObject?.impactDetector?.valuableObject;
                if (valuable != null)
                    if (Protected.Remove(valuable)) MaliTweaks.Logger.LogInfo($"[MT] Unprotected via hurt: {valuable.name}");
            }
        }

        // Unprotect on player contact or contact with player-held object
        [HarmonyPatch(typeof(PhysGrabObjectImpactDetector), nameof(PhysGrabObjectImpactDetector.OnCollisionStay))]
        private static class PhysGrabObjectImpactDetector_OnCollisionStay_Patch
        {
            private static void Prefix(PhysGrabObjectImpactDetector __instance, Collision collision)
            {
                if (!__instance.isValuable) return;
                if (!Protected.Contains(__instance.valuableObject)) return;

                // Direct player contact
                if (collision.transform.CompareTag("Player"))
                {
                    if (Protected.Remove(__instance.valuableObject)) MaliTweaks.Logger.LogInfo($"[MT] Unprotected player collision: {__instance.name}");
                    return;
                }

                // Touched by a player-held object
                var other = collision.gameObject.GetComponent<PhysGrabObject>();
                if (other != null && other.playerGrabbing.Count > 0)
                    if (Protected.Remove(__instance.valuableObject)) MaliTweaks.Logger.LogInfo($"[MT] Unprotected via player held object: {__instance.name}");
            }
        }

        // Apply multiplier after FixedUpdate calculates breakForce
        [HarmonyPatch(typeof(PhysGrabObjectImpactDetector), nameof(PhysGrabObjectImpactDetector.FixedUpdate))]
        private static class PhysGrabObjectImpactDetector_FixedUpdate_Patch
        {
            private static void Postfix(PhysGrabObjectImpactDetector __instance)
            {
                if (!__instance.isValuable) return;
                if (!Protected.Contains(__instance.valuableObject)) return;
                __instance.breakForce *= MaliTweaks.ValuableDmgMult.Value;
            }
        }
    }
}