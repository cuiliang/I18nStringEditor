using System.Configuration;
using System.Data;
using System.Runtime.InteropServices;
using System.Windows;
using I18nStringEditor.Models;
using Microsoft.Win32;
using AppThemeMode = I18nStringEditor.Models.ThemeMode;

namespace I18nStringEditor;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    private static AppThemeMode _currentThemeMode = AppThemeMode.System;

    /// <summary>
    /// 获取当前实际应用的主题（亮色或暗色）
    /// </summary>
    public static bool IsDarkTheme { get; private set; }

    /// <summary>
    /// 获取当前主题模式设置
    /// </summary>
    public static AppThemeMode CurrentThemeMode => _currentThemeMode;

    /// <summary>
    /// 主题变更事件
    /// </summary>
    public static event EventHandler? ThemeChanged;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // 监听系统主题变化
        SystemEvents.UserPreferenceChanged += SystemEvents_UserPreferenceChanged;
    }

    protected override void OnExit(ExitEventArgs e)
    {
        SystemEvents.UserPreferenceChanged -= SystemEvents_UserPreferenceChanged;
        base.OnExit(e);
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

