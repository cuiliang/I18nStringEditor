using System.Collections.ObjectModel;
using System.Collections.Specialized;
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
    /// 仅包含分组子节点（用于TreeView绑定，避免显示叶子节点）
    /// </summary>
    public ObservableCollection<ResourceNode> GroupChildren { get; } = new();

    /// <summary>
    /// 是否是叶子节点（字符串值节点）
    /// </summary>
    public bool IsLeaf => Value != null && Children.Count == 0;

    /// <summary>
    /// 是否有子节点（用于控制展开箭头的显示）
    /// </summary>
    public bool HasChildren => Children.Count > 0;

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
        Children.CollectionChanged += Children_CollectionChanged;
    }

    public ResourceNode(string key, string? value = null) : this()
    {
        Key = key;
        Value = value;
    }

    private void Children_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.Action == NotifyCollectionChangedAction.Add && e.NewItems != null)
        {
            foreach (ResourceNode item in e.NewItems)
            {
                if (!item.IsLeaf)
                {
                    GroupChildren.Add(item);
                }
            }
        }
        else if (e.Action == NotifyCollectionChangedAction.Remove && e.OldItems != null)
        {
            foreach (ResourceNode item in e.OldItems)
            {
                GroupChildren.Remove(item);
            }
        }
        else if (e.Action == NotifyCollectionChangedAction.Reset)
        {
            GroupChildren.Clear();
            foreach (var item in Children)
            {
                if (!item.IsLeaf)
                {
                    GroupChildren.Add(item);
                }
            }
        }
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
