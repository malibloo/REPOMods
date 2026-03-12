using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace MaliTweaks
{
    [BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
    public partial class MaliTweaks : BaseUnityPlugin
    {
        internal static MaliTweaks Instance { get; private set; } = null!;
        internal new static ManualLogSource Logger => Instance._logger;
        private ManualLogSource _logger => base.Logger;
        internal Harmony? Harmony { get; set; }


        // ## Config ##

        // Enemy Valuable Protection
        internal static ConfigEntry<bool> EnemyValProt = null!;
        internal static ConfigEntry<float> EnemyValProtTimer = null!;

        // Zero Grab on Equip
        internal static ConfigEntry<bool> ZeroGravEquip = null!;
        internal static ConfigEntry<float> ZeroGravEquipTimer = null!;

        // Valuable Protection
        internal static ConfigEntry<float> ValuableDmgMult = null!;

        private void Awake()
        {
            Instance = this;

            // Prevent the plugin from being deleted
            gameObject.transform.parent = null;
            gameObject.hideFlags = HideFlags.HideAndDontSave;

            // Config
            EnemyValProt = Config.Bind("General", "EnemyValuableProtection", true, "Protect the enemy valuable from a death pit by resetting the invincibility timer.");
            EnemyValProtTimer = Config.Bind("General", "EnemyValuableProtectionTimer", 5f, "Seconds to set the valuable invincibility timer to when falling in a death pit. Default is 5.0");

            ZeroGravEquip = Config.Bind("General", "ZeroGravOnEquip", true, "Have items be zero grav when pulling them out");
            ZeroGravEquipTimer = Config.Bind("General", "ZeroGravOnEquipTimer", 0.5f, "How long the items will be zero grav for");

            ValuableDmgMult = Config.Bind("General", "ValuableDamageMultiplier", 0.25f, "Physics damage multiplier for valuables that are undiscovered, ungrabbed, and untouched. 0 = invincible, 1 = full damage.\nCan exceed 1 if you're crazy.");

            Patch();

            Logger.LogInfo($"{Info.Metadata.GUID} v{Info.Metadata.Version} has loaded!");
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F8))
            {
                MaliTweaks.Logger.LogInfo($"Protected count: {ValuableProtection.Protected.Count}");
            }
        }

        internal void Patch()
        {
            Harmony ??= new Harmony(Info.Metadata.GUID);
            Harmony.PatchAll();
        }

        internal void Unpatch()
        {
            Harmony?.UnpatchSelf();
        }
    }
}