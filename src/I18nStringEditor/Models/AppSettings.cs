namespace I18nStringEditor.Models;

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
}
