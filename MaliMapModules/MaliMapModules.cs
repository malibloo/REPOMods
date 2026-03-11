using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace MaliMapModules
{
    [BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
    public class MaliMapModules : BaseUnityPlugin
    {
        internal static MaliMapModules Instance { get; private set; } = null!;
        internal new static ManualLogSource Logger => Instance.LoggerInstance;
        private ManualLogSource LoggerInstance => base.Logger;
        internal Harmony? Harmony { get; set; }

        private float _tickTimer;

        // ### Config ###
        internal static ConfigEntry<float> FlushIntervalSeconds = null!;

        // Players
        internal static ConfigEntry<bool> ShowPlayers = null!;
        internal static ConfigEntry<KeyboardShortcut> TogglePlayersKey = null!;
        internal static ConfigEntry<bool> UsePlayerColor = null!;
        internal static ConfigEntry<string> PlayerColorHex = null!;

        // Valuables
        internal static ConfigEntry<bool> ShowValuables = null!;
        internal static ConfigEntry<KeyboardShortcut> ToggleValuablesKey = null!;

        // Enemies
        internal static ConfigEntry<bool> ShowEnemies = null!;
        internal static ConfigEntry<KeyboardShortcut> ToggleEnemiesKey = null!;
        internal static ConfigEntry<string> EnemyColorHex = null!;
        internal static ConfigEntry<bool> UseEnemyRigidbody = null!;

        // Map Colors
        // TODO


        private void Awake()
        {
            Instance = this;

            // Keep alive
            gameObject.transform.parent = null;
            gameObject.hideFlags = HideFlags.HideAndDontSave;

            // Config
            ShowPlayers = Config.Bind("Players", "Enabled", true, "Show player markers on the map.");
            TogglePlayersKey = Config.Bind("Players", "ToggleKey", new KeyboardShortcut(KeyCode.Keypad4), "Toggle visibility of this mod's markers.");
            UsePlayerColor = Config.Bind("Players", "UseColor", true, "Whether to use a player's own color, or stick to a single configurable color");
            PlayerColorHex = Config.Bind("Players", "ColorHex", "#00FFFF", "Override player color to single color. Expects #RRGGBB).");

            ShowValuables = Config.Bind("Valuables", "Enabled", true, "Show all valuables on the map (client-only).");
            ToggleValuablesKey = Config.Bind("Valuables", "ToggleKey", new KeyboardShortcut(KeyCode.Keypad5), "Toggle visibility of this mod's markers.");

            ShowEnemies = Config.Bind("Enemies", "Enabled", false, "Show enemy markers on the map.");
            ToggleEnemiesKey = Config.Bind("Enemies", "ToggleKey", new KeyboardShortcut(KeyCode.Keypad6), "Show all active enemies on the map (client-only).");
            EnemyColorHex = Config.Bind("Enemies", "ColorHex", "#FF0080", "Enemy markers color. Expects #RRGGBB).");
            UseEnemyRigidbody = Config.Bind("Enemies", "UseRigidbody", true, "Whether to attach enemy markers to the Rigidbody (if available) or directly to the GameObject. Attaching to the Rigidbody may provide smoother movement for physics-based enemies, but may cause issues with certain enemy types.");

            FlushIntervalSeconds = Config.Bind("General", "FlushIntervalSeconds", 0.25f, "How often to try flushing pending valuables to the map.");


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

        private void Update()
        {
            if (TogglePlayersKey.Value.IsDown())
            {
                TogglePlayers();
                Logger.LogInfo($"Player markers {(ShowPlayers.Value ? "ON" : "OFF")}");
            }
            
            if (ToggleValuablesKey.Value.IsDown())
            {
                ToggleValuables();
                Logger.LogInfo($"Valuable markers {(ShowValuables.Value ? "ON" : "OFF")}");
            }

            if (ToggleEnemiesKey.Value.IsDown())
            {
                ToggleEnemies();
                Logger.LogInfo($"Enemy markers {(ShowEnemies.Value ? "ON" : "OFF")}");
            }

            _tickTimer += Time.deltaTime;
            if (_tickTimer >= FlushIntervalSeconds.Value)
            {
                _tickTimer = 0f;

                PlayerModules.Tick();
                EnemiesModule.Tick();
                ValuablesModule.Tick();
            }
        }

        private void TogglePlayers(bool? visible = null)
        {
            ShowPlayers.Value = visible ?? !ShowPlayers.Value;
            PlayerModules.UpdateAllMarkers();
        }

        public void ToggleValuables(bool? visible = null)
        {
            ShowValuables.Value = visible ?? !ShowValuables.Value;
            ValuablesModule.UpdateAllMarkers();
        }

        public void ToggleEnemies(bool? visible = null)
        {
            ShowEnemies.Value = visible ?? !ShowEnemies.Value;
            EnemiesModule.UpdateAllMarkers();
        }
    }
}