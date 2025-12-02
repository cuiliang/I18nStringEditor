namespace I18nStringEditor.Models;

/// <summary>
/// 主题模式枚举
/// </summary>
public enum ThemeMode
{
    /// <summary>
    /// 亮色主题
    /// </summary>
    Light,

    /// <summary>
    /// 暗色主题
    /// </summary>
    Dark,

    /// <summary>
    /// 跟随系统
    /// </summary>
    System
}

/// <summary>
/// 应用程序设置
/// </summary>
public class AppSettings
{
    /// <summary>
    /// 上次打开的文件路径
    /// </summary>
    public string? LastOpenedFilePath { get; set; }

    /// <summary>
    /// 窗口宽度
    /// </summary>
    public double WindowWidth { get; set; } = 1200;

    /// <summary>
    /// 窗口高度
    /// </summary>
    public double WindowHeight { get; set; } = 800;

    /// <summary>
    /// 是否显示其他语言面板
    /// </summary>
    public bool ShowOtherLanguagesPanel { get; set; } = true;

    /// <summary>
    /// 左侧面板宽度
    /// </summary>
    public double LeftPanelWidth { get; set; } = 300;

    /// <summary>
    /// 主题模式
    /// </summary>
    public ThemeMode ThemeMode { get; set; } = ThemeMode.System;

    /// <summary>
    /// 是否启用全局快捷键
    /// </summary>
    public bool EnableGlobalHotkey { get; set; } = true;

    /// <summary>
    /// 唤起窗口的全局快捷键 (格式如: "Ctrl + Alt + I")
    /// </summary>
    public string GlobalHotkey { get; set; } = "Ctrl + Alt + I";
}
