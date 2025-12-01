using System.Text.Json.Serialization;

namespace I18nStringEditor.Models;

/// <summary>
/// 存储资源文件的附加信息（保存在.info文件中）
/// </summary>
public class ResourceInfo
{
    /// <summary>
    /// 节点的展开状态，Key为节点完整路径
    /// </summary>
    public Dictionary<string, bool> ExpandedStates { get; set; } = new();

    /// <summary>
    /// 节点的注释信息，Key为节点完整路径
    /// </summary>
    public Dictionary<string, string> Comments { get; set; } = new();

    /// <summary>
    /// 全局设置
    /// </summary>
    public GlobalSettings Settings { get; set; } = new();
}

/// <summary>
/// 全局设置
/// </summary>
public class GlobalSettings
{
    /// <summary>
    /// StringKey模板，用于生成XAML中使用的文本
    /// 使用 {KEY} 作为占位符
    /// </summary>
    public string StringKeyTemplate { get; set; } = "{I18N {x:Static Strings.{KEY}}}";
}
