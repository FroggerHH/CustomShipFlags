using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using ServerSync;
using System;
using System.IO;

#pragma warning disable CS8632
namespace CustomShipFlags
{
    [BepInPlugin(ModGUID, ModName, ModVersion)]
    public class Plugin : BaseUnityPlugin
    {
        #region variables
        public const string ModName = "CustomShipFlags", ModVersion = "1.0.0", ModGUID = "com.Frogger." + ModName;
        private static Harmony harmony = new(ModGUID);
        public static Plugin _self;
        private const string path = "ship/visual/Mast/Sail/sail_full";
        #endregion

        #region ConfigSettings
        static string ConfigFileName = "com.Frogger.CustomShipFlags.cfg";
        DateTime LastConfigChange;
        public static ConfigSync configSync = new(ModName) { DisplayName = ModName, CurrentVersion = ModVersion, MinimumRequiredVersion = ModVersion };
        ConfigEntry<T> config<T>(string group, string name, T value, ConfigDescription description, bool synchronizedSetting = true)
        {
            ConfigEntry<T> configEntry = Config.Bind(group, name, value, description);

            SyncedConfigEntry<T> syncedConfigEntry = configSync.AddConfigEntry(configEntry);
            syncedConfigEntry.SynchronizedConfig = synchronizedSetting;

            return configEntry;
        }

        ConfigEntry<T> config<T>(string group, string name, T value, string description, bool synchronizedSetting = true) => config(group, name, value, new ConfigDescription(description), synchronizedSetting);
        public enum Toggle
        {
            On = 1,
            Off = 0
        }
        
        static ConfigEntry<string> serverFlagUrlConfig;
        static ConfigEntry<Toggle> useOnlyServerFlagConfig;
        public static string serverFlagUrl = "";
        public static bool useOnlyServerFlag = false;
        #endregion

        private void Awake()
        {
            _self = this;
            Config.SaveOnConfigSet = false;
            
            harmony.PatchAll();

            serverFlagUrlConfig = config("General", "FlagURL", "", new ConfigDescription("The server flag url", null, new ConfigurationManagerAttributes { HideSettingName = true, HideDefaultButton = true }));
            useOnlyServerFlagConfig = config("General", "useOnlyServerFlag", Toggle.Off, "");

            SetupWatcher();

            Config.ConfigReloaded += (_, _) => { UpdateConfiguration(); };

            Config.SaveOnConfigSet = true;

            Config.Save();
        }
        private void SetupWatcher()
        {
            FileSystemWatcher fileSystemWatcher = new FileSystemWatcher(Paths.ConfigPath, ConfigFileName);
            fileSystemWatcher.Changed += ConfigChanged;
            fileSystemWatcher.IncludeSubdirectories = true;
            fileSystemWatcher.SynchronizingObject = ThreadingHelper.SynchronizingObject;
            fileSystemWatcher.EnableRaisingEvents = true;
        }
        void ConfigChanged(object sender, FileSystemEventArgs e)
        {
            if ((DateTime.Now - this.LastConfigChange).TotalSeconds <= 5.0)
            {
                return;
            }
            LastConfigChange = DateTime.Now;
            try
            {
                Config.Reload();
                Debug(LogLevel.Info, "Reloading Config...");
            }
            catch
            {
                Debug(LogLevel.Error, "Can't reload Config");
            }
        }
        private void UpdateConfiguration()
        {
            serverFlagUrl = serverFlagUrlConfig.Value;
            useOnlyServerFlag = useOnlyServerFlagConfig.Value == Toggle.On ? true : false;
            Debug(LogLevel.Info, "Configuration Received");
        }

        public void Debug(LogLevel level, string msg)
        {
            Logger.Log(level, msg);
        }
        public void Debug(string msg)
        {
            Logger.LogInfo(msg);
        }

        [HarmonyPatch] public static class Path
        {
            [HarmonyPostfix]
            [HarmonyPatch(typeof(Ship), nameof(Ship.Awake))]
            private static void ShipAwakePatch(Ship __instance)
            {
                __instance.transform.Find(path)?.gameObject.AddComponent<CustomFlagComponent>();
            }
        }

        public class ConfigurationManagerAttributes
        {
            public int? Order;
            public bool? HideSettingName;
            public bool? HideDefaultButton;
            public string? DispName;
            public Action<ConfigEntryBase>? CustomDrawer;
        }
    }
}