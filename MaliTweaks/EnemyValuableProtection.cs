using HarmonyLib;

namespace MaliTweaks
{
    internal static class EnemyValuableProtection
    {
        // Harmony field reference for EnemyValuable.indestructibleTimer
        static readonly AccessTools.FieldRef<EnemyValuable, float> _invTimer =
            AccessTools.FieldRefAccess<EnemyValuable, float>("indestructibleTimer");

        [HarmonyPatch(typeof(PhysGrabObject), nameof(PhysGrabObject.DeathPitEffectCreateRPC))]
        private static class PhysGrabObject_DeathPitEffectCreateRPC_Patch
        {
            private static void Postfix(PhysGrabObject __instance)
            {
                if (MaliTweaks.EnemyValProtTimer.Value != 0f)
                {
                    var valuable = __instance.GetComponent<EnemyValuable>();
                    if (valuable != null)
                    {
                        _invTimer(valuable) = MaliTweaks.EnemyValProtTimer.Value;
                    }
                }
            }
        }
    }
}