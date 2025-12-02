using System.Configuration;
using System.Data;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using I18nStringEditor.Models;
using I18nStringEditor.Services;
using Microsoft.Win32;
using AppThemeMode = I18nStringEditor.Models.ThemeMode;

namespace I18nStringEditor;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    private static AppThemeMode _currentThemeMode = AppThemeMode.System;
    private static GlobalHotkeyService? _globalHotkeyService;
    private static AppSettingsService? _settingsService;
    private static int _activateHotkeyId = -1;

    /// <summary>
    /// 获取当前实际应用的主题（亮色或暗色）
    /// </summary>
    public static bool IsDarkTheme { get; private set; }

    /// <summary>
    /// 获取当前主题模式设置
    /// </summary>
    public static AppThemeMode CurrentThemeMode => _currentThemeMode;

    /// <summary>
    /// 获取全局快捷键服务
    /// </summary>
    public static GlobalHotkeyService? GlobalHotkeyService => _globalHotkeyService;

    /// <summary>
    /// 获取设置服务
    /// </summary>
    public static AppSettingsService? SettingsService => _settingsService;

    /// <summary>
    /// 主题变更事件
    /// </summary>
    public static event EventHandler? ThemeChanged;

    /// <summary>
    /// 快捷键变更事件
    /// </summary>
    public static event EventHandler? HotkeyChanged;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // 初始化设置服务
        _settingsService = new AppSettingsService();
        _settingsService.Load();

        // 监听系统主题变化
        SystemEvents.UserPreferenceChanged += SystemEvents_UserPreferenceChanged;
    }

    protected override void OnExit(ExitEventArgs e)
    {
        SystemEvents.UserPreferenceChanged -= SystemEvents_UserPreferenceChanged;
        
        // 清理全局快捷键服务
        _globalHotkeyService?.Dispose();
        _globalHotkeyService = null;
        
        base.OnExit(e);
    }

    /// <summary>
    /// 初始化全局快捷键（需要在主窗口加载后调用）
    /// </summary>
    public static void InitializeGlobalHotkey(Window mainWindow)
    {
        _globalHotkeyService = new GlobalHotkeyService();
        _globalHotkeyService.Initialize(mainWindow);
        
        // 注册快捷键
        RegisterActivateHotkey();
    }

    /// <summary>
    /// 注册唤起窗口快捷键
    /// </summary>
    public static void RegisterActivateHotkey()
    {
        if (_globalHotkeyService == null || _settingsService == null)
            return;

        // 先注销之前的快捷键
        if (_activateHotkeyId > 0)
        {
            _globalHotkeyService.UnregisterHotkey(_activateHotkeyId);
            _activateHotkeyId = -1;
        }

        // 如果未启用，则不注册
        if (!_settingsService.Settings.EnableGlobalHotkey)
            return;

        // 解析快捷键设置
        var (modifiers, key) = GlobalHotkeyService.ParseHotkeyString(_settingsService.Settings.GlobalHotkey);
        
        if (key != Key.None)
        {
            _activateHotkeyId = _globalHotkeyService.RegisterHotkey(modifiers, key, ActivateMainWindow);
        }

        HotkeyChanged?.Invoke(null, EventArgs.Empty);
    }

    /// <summary>
    /// 唤起主窗口
    /// </summary>
    private static void ActivateMainWindow()
    {
        if (Current.MainWindow == null)
            return;

        var window = Current.MainWindow;
        
        // 如果窗口最小化，恢复它
        if (window.WindowState == WindowState.Minimized)
        {
            window.WindowState = WindowState.Normal;
        }

        // 激活并置顶窗口
        window.Activate();
        window.Topmost = true;
        window.Topmost = false;
        window.Focus();
    }

    /// <summary>
    /// 更新快捷键设置
    /// </summary>
    public static void UpdateHotkeySettings(bool enable, string hotkey)
    {
        if (_settingsService == null)
            return;

        _settingsService.Settings.EnableGlobalHotkey = enable;
        _settingsService.Settings.GlobalHotkey = hotkey;
        _settingsService.Save();

        // 重新注册快捷键
        RegisterActivateHotkey();
    }

    private void SystemEvents_UserPreferenceChanged(object sender, UserPreferenceChangedEventArgs e)
    {
        if (e.Category == UserPreferenceCategory.General)
        {
            // 如果当前是跟随系统模式，则更新主题
            if (_currentThemeMode == AppThemeMode.System)
            {
                ApplyTheme(AppThemeMode.System);
            }
        }
    }

    /// <summary>
    /// 应用主题
    /// </summary>
    /// <param name="mode">主题模式</param>
    public static void ApplyTheme(AppThemeMode mode)
    {
        _currentThemeMode = mode;

        bool isDark = mode switch
        {
            AppThemeMode.Light => false,
            AppThemeMode.Dark => true,
            AppThemeMode.System => IsSystemDarkTheme(),
            _ => false
        };

        IsDarkTheme = isDark;

        // 设置 WPF Fluent 主题模式
        var themeMode = isDark ? System.Windows.ThemeMode.Dark : System.Windows.ThemeMode.Light;
        
        if (Current.MainWindow != null)
        {
            Current.MainWindow.ThemeMode = themeMode;
        }

        // 同时更新应用程序级别的主题
        Current.ThemeMode = themeMode;

        ThemeChanged?.Invoke(null, EventArgs.Empty);
    }

    /// <summary>
    /// 检测系统是否使用暗色主题
    /// </summary>
    private static bool IsSystemDarkTheme()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            if (key != null)
            {
                var value = key.GetValue("AppsUseLightTheme");
                if (value is int intValue)
                {
                    return intValue == 0; // 0 表示暗色主题
                }
            }
        }
        catch
        {
            // 忽略错误，默认使用亮色主题
        }
        return false;
    }

    /// <summary>
    /// 获取主题模式的显示名称
    /// </summary>
    public static string GetThemeModeDisplayName(AppThemeMode mode)
    {
        return mode switch
        {
            AppThemeMode.Light => "亮色",
            AppThemeMode.Dark => "暗色",
            AppThemeMode.System => "跟随系统",
            _ => "未知"
        };
    }
}

