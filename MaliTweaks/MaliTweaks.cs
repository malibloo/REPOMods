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


        private void Awake()
        {
            Instance = this;

            // Prevent the plugin from being deleted
            gameObject.transform.parent = null;
            gameObject.hideFlags = HideFlags.HideAndDontSave;

            ResetValuableDeathPitTimer = Config.Bind("General", "ResetValuableDeahtPitTimer", true, "Reset the valuable invincibility timer when falling in a death pit.");
            ValuableTimerAmount = Config.Bind("General", "ValuableTimerAmount", 5f, "Seconds to set the valuable invincibility timer to when falling in a death pit. Default is 5.0");

            NoGravGrace = Config.Bind("General", "ZeroGravEquip", true, "Have items be zero grav when pulling them out");
            NoGravGraceSeconds = Config.Bind("General", "ZeroGravEquipTimer", 0.5f, "How long the items will be zero grav for");

            Patch();

            Logger.LogInfo($"{Info.Metadata.GUID} v{Info.Metadata.Version} has loaded!");
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