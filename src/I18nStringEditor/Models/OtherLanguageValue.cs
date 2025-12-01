namespace I18nStringEditor.Models;

/// <summary>
/// 表示其他语言文件中的字符串值
/// </summary>
public class OtherLanguageValue
{
    /// <summary>
    /// 语言文件名
    /// </summary>
    public string LanguageFile { get; set; } = string.Empty;

    /// <summary>
    /// 字符串值
    /// </summary>
    public string? Value { get; set; }
}
