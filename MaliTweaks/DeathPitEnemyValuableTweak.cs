using BepInEx.Configuration;
using HarmonyLib;

namespace MaliTweaks
{
    public partial class MaliTweaks
    {
        // Harmony field reference for EnemyValuable.indestructibleTimer
        static readonly AccessTools.FieldRef<EnemyValuable, float> _invTimer =
            AccessTools.FieldRefAccess<EnemyValuable, float>("indestructibleTimer");

        // Config
        internal static ConfigEntry<bool> ResetValuableDeathPitTimer = null!;
        internal static ConfigEntry<float> ValuableTimerAmount = null!;


        [HarmonyPatch(typeof(PhysGrabObject), "DeathPitEffectCreateRPC")]
        private static class PhysGrabObject_DeathPitEffectCreateRPC_Patch
        {
            private static void Postfix(PhysGrabObject __instance)
            {
                if (ResetValuableDeathPitTimer.Value)
                {
                    var valuable = __instance.GetComponent<EnemyValuable>();
                    if (valuable != null)
                    {
                        _invTimer(valuable) = ValuableTimerAmount.Value;
                    }
                }
            }
        }
    }
}