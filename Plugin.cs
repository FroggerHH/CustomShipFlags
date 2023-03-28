using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using ServerSync;
using System;
using System.IO;

#pragma warning disable CS8632
namespace CustomShipFlags;

[BepInPlugin(ModGUID, ModName, ModVersion)]
public class Plugin : BaseUnityPlugin
{
    #region variables
    public const string ModName = "CustomShipFlags", ModVersion = "1.0.0", ModGUID = "com.Frogger." + ModName;
    private static Harmony harmony = new(ModGUID);
    public static Plugin _self;
    internal const string path = "ship/visual/Mast/Sail/sail_full";
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

        #region config
        Config.SaveOnConfigSet = false;
        serverFlagUrlConfig = config("General", "FlagURL", "", new ConfigDescription("The server flag url", null, new ConfigurationManagerAttributes { HideSettingName = true, HideDefaultButton = true }));
        useOnlyServerFlagConfig = config("General", "useOnlyServerFlag", Toggle.Off, "");
        _ = configSync.AddLockingConfigEntry(config("General", "Lock Configuration", true, ""));
        SetupWatcher();
        Config.ConfigReloaded += (_, _) => { UpdateConfiguration(); };
        Config.SettingChanged += (_, _) => { UpdateConfiguration(); };
        Config.SaveOnConfigSet = true;
        Config.Save();
        #endregion

        harmony.PatchAll();
    }

    #region Config
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
            Debug("Reloading Config...");
        }
        catch
        {
            DebugError("Can't reload Config");
        }
    }
    private void UpdateConfiguration()
    {
        serverFlagUrl = serverFlagUrlConfig.Value;
        useOnlyServerFlag = useOnlyServerFlagConfig.Value == Toggle.On ? true : false;
        Debug("Configuration Received");
    }
    #endregion

    #region tools
    public static void Debug(string msg)
    {
        _self.DebugPrivate(msg);
    }
    public static void DebugError(string msg)
    {
        _self.DebugErrorPrivate(msg);
    }
    private void DebugPrivate(string msg)
    {
        Logger.LogInfo(msg);
    }
    private void DebugErrorPrivate(string msg)
    {
        Logger.LogError($"{msg} Write to the developer and moderator if this happens often.");
    }
    #endregion
}