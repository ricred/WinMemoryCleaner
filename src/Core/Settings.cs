using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Windows.Input;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace WinMemoryCleaner
{
    public static class Settings
    {
        private static readonly CultureInfo _culture = new CultureInfo(Constants.Windows.Locale.Name.English);
        private static readonly object _settingsLock = new object();
        private static Timer _saveDebounceTimer;

        // The auto optimization interval is stored in minutes. Legacy versions stored hours under the "AutoOptimizationInterval" value name.
        private const string AutoOptimizationMinutesValueName = "AutoOptimizationIntervalMinutes";

        #region Constructors

        static Settings()
        {
            Load();
            Save();
        }

        #endregion

        #region Properties

        public static bool AlwaysOnTop { get; set; }

        public static int AutoOptimizationInterval { get; set; }

        public static int AutoOptimizationMemoryUsage { get; set; }

        public static bool AutoUpdate { get; set; }

        public static bool CloseAfterOptimization { get; set; }

        public static bool CloseToTheNotificationArea { get; set; }

        public static bool CompactMode { get; set; }

        public static bool CreateStartMenuShortcut { get; set; }

        public static double FontSize { get; set; }

        public static string Language { get; set; }

        public static Enums.Memory.Areas MemoryAreas { get; set; }

        public static Key OptimizationKey { get; set; }

        public static ModifierKeys OptimizationModifiers { get; set; }

        public static SortedSet<string> ProcessExclusionList { get; private set; }

        public static Enums.Priority RunOnPriority { get; set; }

        public static bool RunOnStartup { get; set; }

        public static bool ShowOptimizationNotifications { get; set; }

        public static bool ShowVirtualMemory { get; set; }

        public static bool StartMinimized { get; set; }

        public static Brush TrayIconBackgroundColor { get; set; }

        public static Brush TrayIconDangerColor { get; set; }

        public static byte TrayIconDangerLevel { get; set; }

        public static bool TrayIconOptimizeOnMiddleMouseClick { get; set; }

        public static Brush TrayIconOptimizingColor { get; set; }

        public static bool TrayIconShowMemoryUsage { get; set; }

        public static Brush TrayIconTextColor { get; set; }

        public static bool TrayIconUseTransparentBackground { get; set; }

        public static Brush TrayIconWarningColor { get; set; }

        public static byte TrayIconWarningLevel { get; set; }

        public static bool UseHotkey { get; set; }

        #endregion

        #region Methods

        private static void Load(bool loadUserValues = true)
        {
            // Default values
            AlwaysOnTop = false;
            AutoOptimizationInterval = 0;
            AutoOptimizationMemoryUsage = 0;
            AutoUpdate = true;
            CloseAfterOptimization = false;
            CloseToTheNotificationArea = false;
            CompactMode = false;
            CreateStartMenuShortcut = true;
            FontSize = 14;
            Language = Constants.Windows.Locale.Name.English;
            MemoryAreas = Enums.Memory.Areas.CombinedPageList | Enums.Memory.Areas.ModifiedFileCache | Enums.Memory.Areas.ModifiedPageList | Enums.Memory.Areas.RegistryCache | Enums.Memory.Areas.StandbyList | Enums.Memory.Areas.SystemFileCache | Enums.Memory.Areas.WorkingSet;
            OptimizationKey = Key.M;
            OptimizationModifiers = ModifierKeys.Control | ModifierKeys.Shift;
            ProcessExclusionList = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
            RunOnPriority = Enums.Priority.Low;
            RunOnStartup = false;
            ShowOptimizationNotifications = true;
            ShowVirtualMemory = false;
            StartMinimized = false;
            TrayIconBackgroundColor = Brushes.DarkGreen;
            TrayIconDangerColor = Brushes.DarkRed;
            TrayIconDangerLevel = 90;
            TrayIconOptimizeOnMiddleMouseClick = false;
            TrayIconOptimizingColor = Brushes.DimGray;
            TrayIconShowMemoryUsage = false;
            TrayIconTextColor = Brushes.White;
            TrayIconUseTransparentBackground = false;
            TrayIconWarningColor = Brushes.DarkGoldenrod;
            TrayIconWarningLevel = 80;
            UseHotkey = false;

            // User values
            try
            {
                if (!loadUserValues)
                    return;

                // Process Exclusion List
                using (var key = Registry.LocalMachine.OpenSubKey(Constants.App.Registry.Key.ProcessExclusionList))
                {
                    if (key != null)
                    {
                        foreach (var name in key.GetValueNames())
                            ProcessExclusionList.Add(name.RemoveWhitespaces().Replace(".exe", string.Empty).ToLower(_culture));
                    }
                }

                // Settings
                using (var key = Registry.LocalMachine.OpenSubKey(Constants.App.Registry.Key.Settings))
                {
                    if (key != null)
                    {
                        AlwaysOnTop = Convert.ToBoolean(key.GetValue(Helper.NameOf(() => AlwaysOnTop), AlwaysOnTop), _culture);
                        AutoOptimizationInterval = Convert.ToInt32(key.GetValue(AutoOptimizationMinutesValueName, Convert.ToInt32(key.GetValue(Helper.NameOf(() => AutoOptimizationInterval), 0), _culture) * 60), _culture);
                        AutoOptimizationMemoryUsage = Convert.ToInt32(key.GetValue(Helper.NameOf(() => AutoOptimizationMemoryUsage), AutoOptimizationMemoryUsage), _culture);
                        AutoUpdate = Convert.ToBoolean(key.GetValue(Helper.NameOf(() => AutoUpdate), AutoUpdate), _culture);
                        CloseAfterOptimization = Convert.ToBoolean(key.GetValue(Helper.NameOf(() => CloseAfterOptimization), CloseAfterOptimization), _culture);
                        CloseToTheNotificationArea = Convert.ToBoolean(key.GetValue(Helper.NameOf(() => CloseToTheNotificationArea), CloseToTheNotificationArea), _culture);
                        CompactMode = Convert.ToBoolean(key.GetValue(Helper.NameOf(() => CompactMode), CompactMode), _culture);
                        CreateStartMenuShortcut = Convert.ToBoolean(key.GetValue(Helper.NameOf(() => CreateStartMenuShortcut), CreateStartMenuShortcut), _culture);
                        FontSize = Convert.ToDouble(key.GetValue(Helper.NameOf(() => FontSize), FontSize), _culture);
                        Language = Convert.ToString(key.GetValue(Helper.NameOf(() => Language), Language), CultureInfo.InvariantCulture);

                        Enums.Memory.Areas memoryAreas;

                        if (Enum.TryParse(Convert.ToString(key.GetValue(Helper.NameOf(() => MemoryAreas), MemoryAreas), _culture), out memoryAreas) && memoryAreas.IsValid())
                        {
                            if ((memoryAreas & Enums.Memory.Areas.StandbyList) != 0 && (memoryAreas & Enums.Memory.Areas.StandbyListLowPriority) != 0)
                                memoryAreas &= ~Enums.Memory.Areas.StandbyListLowPriority;

                            MemoryAreas = memoryAreas;
                        }

                        Key optimizationKey;

                        if (Enum.TryParse(Convert.ToString(key.GetValue(Helper.NameOf(() => OptimizationKey), OptimizationKey), _culture), out optimizationKey) && optimizationKey.IsValid())
                            OptimizationKey = optimizationKey;

                        ModifierKeys optimizationModifiers;

                        if (Enum.TryParse(Convert.ToString(key.GetValue(Helper.NameOf(() => OptimizationModifiers), OptimizationModifiers), _culture), out optimizationModifiers) && optimizationModifiers.IsValid())
                            OptimizationModifiers = optimizationModifiers;

                        Enums.Priority runOnPriority;

                        if (Enum.TryParse(Convert.ToString(key.GetValue(Helper.NameOf(() => RunOnPriority), RunOnPriority), _culture), out runOnPriority) && runOnPriority.IsValid())
                            RunOnPriority = runOnPriority;

                        RunOnStartup = Convert.ToBoolean(key.GetValue(Helper.NameOf(() => RunOnStartup), RunOnStartup), _culture);
                        ShowOptimizationNotifications = Convert.ToBoolean(key.GetValue(Helper.NameOf(() => ShowOptimizationNotifications), ShowOptimizationNotifications), _culture);
                        ShowVirtualMemory = Convert.ToBoolean(key.GetValue(Helper.NameOf(() => ShowVirtualMemory), ShowVirtualMemory), _culture);
                        StartMinimized = Convert.ToBoolean(key.GetValue(Helper.NameOf(() => StartMinimized), StartMinimized), _culture);
                        TrayIconBackgroundColor = Convert.ToString(key.GetValue(Helper.NameOf(() => TrayIconBackgroundColor), TrayIconBackgroundColor), _culture).ToBrush(TrayIconBackgroundColor);
                        TrayIconDangerColor = Convert.ToString(key.GetValue(Helper.NameOf(() => TrayIconDangerColor), TrayIconDangerColor), _culture).ToBrush(TrayIconDangerColor);
                        TrayIconDangerLevel = Convert.ToByte(key.GetValue(Helper.NameOf(() => TrayIconDangerLevel), TrayIconDangerLevel), _culture);
                        TrayIconOptimizeOnMiddleMouseClick = Convert.ToBoolean(key.GetValue(Helper.NameOf(() => TrayIconOptimizeOnMiddleMouseClick), TrayIconOptimizeOnMiddleMouseClick), _culture);
                        TrayIconOptimizingColor = Convert.ToString(key.GetValue(Helper.NameOf(() => TrayIconOptimizingColor), TrayIconOptimizingColor), _culture).ToBrush(TrayIconOptimizingColor);
                        TrayIconShowMemoryUsage = Convert.ToBoolean(key.GetValue(Helper.NameOf(() => TrayIconShowMemoryUsage), TrayIconShowMemoryUsage), _culture);
                        TrayIconTextColor = Convert.ToString(key.GetValue(Helper.NameOf(() => TrayIconTextColor), TrayIconTextColor), _culture).ToBrush(TrayIconTextColor);
                        TrayIconUseTransparentBackground = Convert.ToBoolean(key.GetValue(Helper.NameOf(() => TrayIconUseTransparentBackground), TrayIconUseTransparentBackground), _culture);
                        TrayIconWarningColor = Convert.ToString(key.GetValue(Helper.NameOf(() => TrayIconWarningColor), TrayIconWarningColor), _culture).ToBrush(TrayIconWarningColor);
                        TrayIconWarningLevel = Convert.ToByte(key.GetValue(Helper.NameOf(() => TrayIconWarningLevel), TrayIconWarningLevel), _culture);
                        UseHotkey = Convert.ToBoolean(key.GetValue(Helper.NameOf(() => UseHotkey), UseHotkey), _culture);
                    }
                    else
                    {
                        // Smart language setter for the first run
                        var culture = CultureInfo.CurrentCulture;
                        var languages = Localizer.Languages.Select(language => language.Name).ToList();

                        do
                        {
                            if (languages.Contains(culture.Name, StringComparer.OrdinalIgnoreCase))
                            {
                                Localizer.Language = new Language(culture);
                                Language = culture.Name;
                                break;
                            }

                            culture = culture.Parent;
                        }
                        while (culture.LCID != CultureInfo.InvariantCulture.LCID);
                    }
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }
        }

        /// <summary>
        /// Saves settings asynchronously with debouncing to avoid blocking the UI thread.
        /// Rapid successive calls (slider drags, etc.) collapse into a single registry write
        /// that always persists the values current at fire time.
        /// </summary>
        public static void SaveAsync()
        {
            lock (_settingsLock)
            {
                if (_saveDebounceTimer == null)
                    _saveDebounceTimer = new Timer(OnSaveDebounceElapsed, null, Timeout.Infinite, Timeout.Infinite);

                // Re-arm: every call pushes the save 500ms into the future
                _saveDebounceTimer.Change(500, Timeout.Infinite);
            }
        }

        private static void OnSaveDebounceElapsed(object state)
        {
            try
            {
                SaveInternal();
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
        }

        /// <summary>
        /// Internal save method that performs the actual registry I/O.
        /// </summary>
        private static void SaveInternal()
        {
            SaveInternal(
                AlwaysOnTop, AutoOptimizationInterval, AutoOptimizationMemoryUsage, AutoUpdate,
                CloseAfterOptimization, CloseToTheNotificationArea, CompactMode, CreateStartMenuShortcut,
                FontSize, Language, MemoryAreas, OptimizationKey, OptimizationModifiers, RunOnPriority,
                RunOnStartup, ShowOptimizationNotifications, ShowVirtualMemory, StartMinimized,
                TrayIconBackgroundColor, TrayIconDangerColor, TrayIconDangerLevel, TrayIconOptimizeOnMiddleMouseClick,
                TrayIconOptimizingColor, TrayIconShowMemoryUsage, TrayIconTextColor, TrayIconUseTransparentBackground,
                TrayIconWarningColor, TrayIconWarningLevel, UseHotkey, ProcessExclusionList);
        }

        /// <summary>
        /// Internal save method that performs the actual registry I/O with captured values.
        /// </summary>
        private static void SaveInternal(
            bool alwaysOnTop, int autoOptimizationInterval, int autoOptimizationMemoryUsage, bool autoUpdate,
            bool closeAfterOptimization, bool closeToTheNotificationArea, bool compactMode, bool createStartMenuShortcut,
            double fontSize, string language, Enums.Memory.Areas memoryAreas, Key optimizationKey, ModifierKeys optimizationModifiers,
            Enums.Priority runOnPriority, bool runOnStartup, bool showOptimizationNotifications, bool showVirtualMemory,
            bool startMinimized, Brush trayIconBackgroundColor, Brush trayIconDangerColor, byte trayIconDangerLevel,
            bool trayIconOptimizeOnMiddleMouseClick, Brush trayIconOptimizingColor, bool trayIconShowMemoryUsage,
            Brush trayIconTextColor, bool trayIconUseTransparentBackground, Brush trayIconWarningColor,
            byte trayIconWarningLevel, bool useHotkey, SortedSet<string> processExclusionList)
        {
            try
            {
                // Process Exclusion List
                Registry.LocalMachine.DeleteSubKey(Constants.App.Registry.Key.ProcessExclusionList, false);

                if (processExclusionList.Any())
                {
                    using (var key = Registry.LocalMachine.CreateSubKey(Constants.App.Registry.Key.ProcessExclusionList))
                    {
                        if (key != null)
                        {
                            foreach (var process in processExclusionList)
                                key.SetValue(process.RemoveWhitespaces().Replace(".exe", string.Empty).ToLower(_culture), string.Empty, RegistryValueKind.String);
                        }
                    }
                }

                // Settings
                using (var key = Registry.LocalMachine.CreateSubKey(Constants.App.Registry.Key.Settings))
                {
                    if (key != null)
                    {
                        key.SetValue(Helper.NameOf(() => AlwaysOnTop), alwaysOnTop ? 1 : 0);
                        key.SetValue(AutoOptimizationMinutesValueName, autoOptimizationInterval);

                        // Remove the legacy hours-based value so the minutes-based value is the single source of truth
                        key.DeleteValue(Helper.NameOf(() => AutoOptimizationInterval), false);
                        key.SetValue(Helper.NameOf(() => AutoOptimizationMemoryUsage), autoOptimizationMemoryUsage);
                        key.SetValue(Helper.NameOf(() => AutoUpdate), autoUpdate ? 1 : 0);
                        key.SetValue(Helper.NameOf(() => CloseAfterOptimization), closeAfterOptimization ? 1 : 0);
                        key.SetValue(Helper.NameOf(() => CloseToTheNotificationArea), closeToTheNotificationArea ? 1 : 0);
                        key.SetValue(Helper.NameOf(() => CompactMode), compactMode ? 1 : 0);
                        key.SetValue(Helper.NameOf(() => CreateStartMenuShortcut), createStartMenuShortcut ? 1 : 0);
                        key.SetValue(Helper.NameOf(() => FontSize), fontSize);
                        key.SetValue(Helper.NameOf(() => Language), language);
                        key.SetValue(Helper.NameOf(() => MemoryAreas), (int)memoryAreas);
                        key.SetValue(Helper.NameOf(() => OptimizationKey), (int)optimizationKey);
                        key.SetValue(Helper.NameOf(() => OptimizationModifiers), (int)optimizationModifiers);
                        key.SetValue(Helper.NameOf(() => RunOnPriority), (int)runOnPriority);
                        key.SetValue(Helper.NameOf(() => RunOnStartup), runOnStartup ? 1 : 0);
                        key.SetValue(Helper.NameOf(() => ShowOptimizationNotifications), showOptimizationNotifications ? 1 : 0);
                        key.SetValue(Helper.NameOf(() => ShowVirtualMemory), showVirtualMemory ? 1 : 0);
                        key.SetValue(Helper.NameOf(() => StartMinimized), startMinimized ? 1 : 0);
                        key.SetValue(Helper.NameOf(() => TrayIconBackgroundColor), trayIconBackgroundColor.GetHex(true));
                        key.SetValue(Helper.NameOf(() => TrayIconDangerColor), trayIconDangerColor.GetHex(true));
                        key.SetValue(Helper.NameOf(() => TrayIconDangerLevel), trayIconDangerLevel);
                        key.SetValue(Helper.NameOf(() => TrayIconOptimizeOnMiddleMouseClick), trayIconOptimizeOnMiddleMouseClick ? 1 : 0);
                        key.SetValue(Helper.NameOf(() => TrayIconOptimizingColor), trayIconOptimizingColor.GetHex(true));
                        key.SetValue(Helper.NameOf(() => TrayIconShowMemoryUsage), trayIconShowMemoryUsage ? 1 : 0);
                        key.SetValue(Helper.NameOf(() => TrayIconTextColor), trayIconTextColor.GetHex(true));
                        key.SetValue(Helper.NameOf(() => TrayIconUseTransparentBackground), trayIconUseTransparentBackground ? 1 : 0);
                        key.SetValue(Helper.NameOf(() => TrayIconWarningColor), trayIconWarningColor.GetHex(true));
                        key.SetValue(Helper.NameOf(() => TrayIconWarningLevel), trayIconWarningLevel);
                        key.SetValue(Helper.NameOf(() => UseHotkey), useHotkey ? 1 : 0);
                    }
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }
        }

        public static void Reset(bool keepLanguage = false)
        {
            var language = Language;

            Load(false);

            if (keepLanguage)
                Language = language;

            Save();
        }

        public static void Save()
        {
            SaveInternal();
        }

        #endregion
    }
}

#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
