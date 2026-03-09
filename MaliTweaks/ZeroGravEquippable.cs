using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

namespace MaliTweaks
{
    public partial class MaliTweaks
    {
        // Config
        internal static ConfigEntry<bool> NoGravGrace = null!;
        internal static ConfigEntry<float> NoGravGraceSeconds = null!;


        [HarmonyPatch(typeof(PhysGrabObject), "OverrideTimersTick")]
        private static class PhysGrabObject_OverrideTimersTick_Patch
        {
            [HarmonyPostfix]
            private static void Postfix(PhysGrabObject __instance)
            {
                if (!NoGravGrace.Value || __instance?.rb == null) return;

                var state = __instance.GetComponent<NoGravState>();
                if (state == null) return;

                if (state.Remaining > 0f)
                {
                    state.Remaining -= Time.deltaTime;
                    if (state.Remaining < 0f) state.Remaining = 0f;

                    if (state.Remaining > 0f)
                    {
                        __instance.rb.useGravity = false;
                        state.WasActive = true;
                        return;
                    }
                    // else: fell through, just ended this tick
                }

                // Not active now - restore once if we were active before
                if (!state.WasActive) return;

                state.WasActive = false;
                if (__instance.timerZeroGravity > 0f) return;
                __instance.rb.useGravity = true;
            }
        }

        [HarmonyPatch(typeof(ItemEquippable), "RPC_CompleteUnequip")]
        private static class ItemEquippable_RPC_CompleteUnequip_Patch
        {
            [HarmonyPostfix]
            private static void Postfix(ItemEquippable __instance)
            {
                var go = __instance.gameObject;
                if (!NoGravGrace.Value || go == null) return;


                var state = go.GetComponent<NoGravState>();
                state ??= go.gameObject.AddComponent<NoGravState>();

                state.Remaining = NoGravGraceSeconds.Value;
                state.WasActive = false;
            }
        }

        [HarmonyPatch(typeof(PhysGrabObject), "FixedUpdate")]
        private static class PhysGrabObject_FixedUpdate_Patch
        {
            [HarmonyPostfix]
            private static void Postfix(PhysGrabObject __instance)
            {
                if (__instance == null || !NoGravGrace.Value) return;
                var state = __instance.GetComponent<NoGravState>();
                if (state == null) return;

                // Is timer active?
                if (state.Remaining <= 0f) return;

                // Is it being grabbed?
                if (!__instance.grabbedLocal && !__instance.grabbed) return;

                // Cancel grav timer through grab
                state.Remaining = 0f;

                // Restore gravity only if game's zero grav timer isn't active
                if (state.WasActive && __instance.rb != null)
                {
                    if (__instance.timerZeroGravity <= 0f)
                        __instance.rb.useGravity = true;

                    state.WasActive = false;
                }
            }

        }
    }
    public sealed class NoGravState : MonoBehaviour
    {
        public float Remaining;
        public bool WasActive;
    }
}
