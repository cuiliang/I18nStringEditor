using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace I18nStringEditor.Models;

/// <summary>
/// 表示资源树中的一个节点（可以是分组节点或叶子节点）
/// </summary>
public partial class ResourceNode : ObservableObject
{
    [ObservableProperty]
    private string _key = string.Empty;

    [ObservableProperty]
    private string? _value;

    [ObservableProperty]
    private string? _comment;

    [ObservableProperty]
    private bool _isExpanded = true;

    [ObservableProperty]
    private bool _isSelected;

    public ResourceNode? Parent { get; set; }

    public ObservableCollection<ResourceNode> Children { get; } = new();

    /// <summary>
    /// 是否是叶子节点（字符串值节点）
    /// </summary>
    public bool IsLeaf => Value != null && Children.Count == 0;

    /// <summary>
    /// 获取完整的路径键（如：Areas.Accounts.Common.LocalAccountTitle）
    /// </summary>
    public string FullPath
    {
        get
        {
            // 如果没有父节点，或父节点是根节点（Root），则直接返回 Key
            if (Parent == null || Parent.Key == "Root")
                return Key;
            return $"{Parent.FullPath}.{Key}";
        }
    }

    /// <summary>
    /// 获取用于XAML的StringKey格式
    /// </summary>
    public string StringKey => FullPath.Replace(".", "_");

    public ResourceNode()
    {
    }

    public ResourceNode(string key, string? value = null)
    {
        Key = key;
        Value = value;
    }

    public void AddChild(ResourceNode child)
    {
        child.Parent = this;
        Children.Add(child);
    }

    public void RemoveChild(ResourceNode child)
    {
        child.Parent = null;
        Children.Remove(child);
    }

    public void SortChildren()
    {
        var sorted = Children.OrderBy(c => c.Key).ToList();
        Children.Clear();
        foreach (var child in sorted)
        {
            Children.Add(child);
        }
    }
}
