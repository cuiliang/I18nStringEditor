using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using I18nStringEditor.Models;
using I18nStringEditor.Services;
using Microsoft.Win32;
using AppThemeMode = I18nStringEditor.Models.ThemeMode;

namespace I18nStringEditor.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly ResourceFileService _resourceService;
    private readonly AppSettingsService _settingsService;
    private System.Timers.Timer? _autoSaveTimer;

    [ObservableProperty]
    private string _windowTitle = "字符串管理";

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private bool _showOtherLanguagesPanel = true;

    [ObservableProperty]
    private ResourceNode? _selectedTreeNode;

    [ObservableProperty]
    private ResourceNode? _selectedStringItem;

    [ObservableProperty]
    private string _statusMessage = "就绪";

    [ObservableProperty]
    private string _stringKeyTemplate = "{I18N {x:Static Strings.{KEY}}}";

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _hasSearchResults;

    [ObservableProperty]
    private AppThemeMode _currentThemeMode = AppThemeMode.System;

    /// <summary>
    /// 树形节点集合（显示分组节点）
    /// </summary>
    public ObservableCollection<ResourceNode> TreeNodes { get; } = new();

    /// <summary>
    /// 当前选中节点下的字符串列表
    /// </summary>
    public ObservableCollection<ResourceNode> StringItems { get; } = new();

    /// <summary>
    /// 搜索结果
    /// </summary>
    public ObservableCollection<ResourceNode> SearchResults { get; } = new();

    /// <summary>
    /// 其他语言的值
    /// </summary>
    public ObservableCollection<OtherLanguageValue> OtherLanguageValues { get; } = new();

    public MainViewModel()
    {
        _resourceService = new ResourceFileService();
        _settingsService = new AppSettingsService();

        // 加载设置
        _settingsService.Load();
        ShowOtherLanguagesPanel = _settingsService.Settings.ShowOtherLanguagesPanel;
        CurrentThemeMode = _settingsService.Settings.ThemeMode;

        // 应用主题
        App.ApplyTheme(CurrentThemeMode);
        App.ThemeChanged += OnThemeChanged;

        // 更新窗口标题
        UpdateWindowTitle();

        // 设置自动保存定时器
        _autoSaveTimer = new System.Timers.Timer(2000); // 2秒后自动保存
        _autoSaveTimer.AutoReset = false;
        _autoSaveTimer.Elapsed += async (s, e) =>
        {
            await Application.Current.Dispatcher.InvokeAsync(async () =>
            {
                await SaveFileAsync();
            });
        };
    }

    private void OnThemeChanged(object? sender, EventArgs e)
    {
        UpdateWindowTitle();
    }

    public async Task InitializeAsync()
    {
        // 尝试加载上次打开的文件
        if (!string.IsNullOrEmpty(_settingsService.Settings.LastOpenedFilePath) &&
            File.Exists(_settingsService.Settings.LastOpenedFilePath))
        {
            await LoadFileAsync(_settingsService.Settings.LastOpenedFilePath);
        }
    }

    partial void OnCurrentThemeModeChanged(AppThemeMode value)
    {
        _settingsService.Settings.ThemeMode = value;
        _settingsService.Save();
        App.ApplyTheme(value);
    }

    /// <summary>
    /// 更新窗口标题
    /// </summary>
    private void UpdateWindowTitle()
    {
        var themeText = App.GetThemeModeDisplayName(CurrentThemeMode);
        var themeIndicator = App.IsDarkTheme ? "🌙" : "☀️";
        
        if (!string.IsNullOrEmpty(_settingsService.Settings.LastOpenedFilePath))
        {
            WindowTitle = $"{_settingsService.Settings.LastOpenedFilePath}";
        }
        else
        {
            WindowTitle = $"字符串管理";
        }
    }

    /// <summary>
    /// 获取主题模式显示名称
    /// </summary>
    public string GetThemeDisplayName(AppThemeMode mode) => App.GetThemeModeDisplayName(mode);

    partial void OnSelectedTreeNodeChanged(ResourceNode? value)
    {
        UpdateStringItems();
    }

    partial void OnSelectedStringItemChanged(ResourceNode? value)
    {
        _ = LoadOtherLanguageValuesAsync();
    }

    partial void OnSearchTextChanged(string value)
    {
        PerformSearch();
    }

    partial void OnShowOtherLanguagesPanelChanged(bool value)
    {
        _settingsService.Settings.ShowOtherLanguagesPanel = value;
        _settingsService.Save();
    }

    private void UpdateStringItems()
    {
        StringItems.Clear();
        OtherLanguageValues.Clear();

        if (SelectedTreeNode == null)
            return;

        // 添加当前节点下的所有叶子节点（字符串）
        foreach (var child in SelectedTreeNode.Children)
        {
            if (child.IsLeaf)
            {
                StringItems.Add(child);
            }
        }
    }

    private async Task LoadOtherLanguageValuesAsync()
    {
        OtherLanguageValues.Clear();

        if (SelectedStringItem == null)
            return;

        var values = await _resourceService.GetOtherLanguageValuesAsync(SelectedStringItem.FullPath);
        foreach (var value in values)
        {
            OtherLanguageValues.Add(value);
        }
    }

    private void PerformSearch()
    {
        SearchResults.Clear();

        if (string.IsNullOrWhiteSpace(SearchText))
        {
            HasSearchResults = false;
            return;
        }

        var results = _resourceService.Search(SearchText);
        foreach (var result in results)
        {
            SearchResults.Add(result);
        }
        HasSearchResults = SearchResults.Count > 0;
    }

    [RelayCommand]
    private async Task OpenFileAsync()
    {
        var dialog = new OpenFileDialog
        {
            Filter = "JSON 文件|*.json|所有文件|*.*",
            Title = "打开资源文件"
        };

        if (dialog.ShowDialog() == true)
        {
            await LoadFileAsync(dialog.FileName);
        }
    }

    private async Task LoadFileAsync(string filePath)
    {
        try
        {
            IsLoading = true;
            StatusMessage = "正在加载...";

            var rootNode = await _resourceService.LoadAsync(filePath);
            if (rootNode != null)
            {
                TreeNodes.Clear();
                foreach (var child in rootNode.Children)
                {
                    if (!child.IsLeaf)
                    {
                        TreeNodes.Add(child);
                    }
                }

                // 更新设置
                _settingsService.Settings.LastOpenedFilePath = filePath;
                _settingsService.Save();

                // 更新标题（包含主题信息）
                UpdateWindowTitle();

                // 加载StringKey模板
                if (_resourceService.CurrentInfo != null)
                {
                    StringKeyTemplate = _resourceService.CurrentInfo.Settings.StringKeyTemplate;
                }

                // 恢复选中状态
                RestoreSelectedStates();

                StatusMessage = "文件加载成功";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"加载失败: {ex.Message}";
            MessageBox.Show($"加载文件失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// 恢复选中状态
    /// </summary>
    private void RestoreSelectedStates()
    {
        var (treeNodePath, stringItemPath) = _resourceService.GetSelectedPaths();

        // 恢复树节点选中状态
        if (!string.IsNullOrEmpty(treeNodePath))
        {
            var treeNode = _resourceService.FindNodeByPath(treeNodePath);
            if (treeNode != null)
            {
                // 展开所有祖先节点
                var ancestorNode = treeNode.Parent;
                while (ancestorNode != null && ancestorNode != _resourceService.RootNode)
                {
                    ancestorNode.IsExpanded = true;
                    ancestorNode = ancestorNode.Parent;
                }

                treeNode.IsSelected = true;
                SelectedTreeNode = treeNode;
            }
        }

        // 恢复字符串项选中状态
        if (!string.IsNullOrEmpty(stringItemPath))
        {
            var stringItem = _resourceService.FindNodeByPath(stringItemPath);
            if (stringItem != null)
            {
                SelectedStringItem = stringItem;
            }
        }
    }

    [RelayCommand]
    private async Task SaveFileAsync()
    {
        try
        {
            // 保存StringKey模板
            if (_resourceService.CurrentInfo != null)
            {
                _resourceService.CurrentInfo.Settings.StringKeyTemplate = StringKeyTemplate;
            }

            // 保存选中状态
            _resourceService.SetSelectedPaths(
                SelectedTreeNode?.FullPath,
                SelectedStringItem?.FullPath);

            await _resourceService.SaveAsync();
            StatusMessage = $"已保存 - {DateTime.Now:HH:mm:ss}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"保存失败: {ex.Message}";
        }
    }

    [RelayCommand]
    private void AddGroup()
    {
        if (SelectedTreeNode == null && TreeNodes.Count == 0)
        {
            // 如果没有根节点，先创建
            StatusMessage = "请先打开一个资源文件";
            return;
        }

        var parentNode = SelectedTreeNode ?? _resourceService.RootNode;
        if (parentNode == null)
            return;

        var dialog = new InputDialog("添加分组", "请输入分组名称:");
        dialog.Owner = Application.Current.MainWindow;
        if (dialog.ShowDialog() == true && !string.IsNullOrWhiteSpace(dialog.InputText))
        {
            var newNode = _resourceService.CreateNode(parentNode, dialog.InputText);
            if (parentNode == _resourceService.RootNode)
            {
                TreeNodes.Add(newNode);
            }
            TriggerAutoSave();
        }
    }

    [RelayCommand]
    private void AddString()
    {
        if (SelectedTreeNode == null)
        {
            StatusMessage = "请先选择一个分组节点";
            return;
        }

        var dialog = new AddStringDialog();
        dialog.Owner = Application.Current.MainWindow;
        if (dialog.ShowDialog() == true)
        {
            var newNode = _resourceService.CreateNode(SelectedTreeNode, dialog.StringKey, dialog.StringValue);
            newNode.Comment = dialog.StringComment;
            StringItems.Add(newNode);
            TriggerAutoSave();

            // 自动复制生成的 Key 到剪贴板
            var stringKey = StringKeyTemplate.Replace("{KEY}", newNode.StringKey);
            Clipboard.SetText(stringKey);
            StatusMessage = $"已添加新字符串，Key已复制: {stringKey}";
        }
    }

    [RelayCommand]
    private void DeleteSelectedString()
    {
        if (SelectedStringItem == null)
            return;

        var result = MessageBox.Show(
            $"确定要删除字符串 \"{SelectedStringItem.Key}\" 吗？",
            "确认删除",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result == MessageBoxResult.Yes)
        {
            _resourceService.DeleteNode(SelectedStringItem);
            StringItems.Remove(SelectedStringItem);
            SelectedStringItem = null;
            TriggerAutoSave();
        }
    }

    [RelayCommand]
    private void DeleteSelectedGroup()
    {
        if (SelectedTreeNode == null)
            return;

        var result = MessageBox.Show(
            $"确定要删除分组 \"{SelectedTreeNode.Key}\" 及其所有内容吗？",
            "确认删除",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result == MessageBoxResult.Yes)
        {
            var parent = SelectedTreeNode.Parent;
            _resourceService.DeleteNode(SelectedTreeNode);

            if (parent == _resourceService.RootNode)
            {
                TreeNodes.Remove(SelectedTreeNode);
            }

            SelectedTreeNode = null;
            StringItems.Clear();
            TriggerAutoSave();
        }
    }

    [RelayCommand]
    private void SortNodes()
    {
        if (SelectedTreeNode != null)
        {
            SelectedTreeNode.SortChildren();
            UpdateStringItems();
            TriggerAutoSave();
        }
    }

    [RelayCommand]
    private void CopyStringKey()
    {
        if (SelectedStringItem == null)
            return;

        var stringKey = StringKeyTemplate.Replace("{KEY}", SelectedStringItem.StringKey);
        Clipboard.SetText(stringKey);
        StatusMessage = $"已复制: {stringKey}";
    }

    [RelayCommand]
    private void ToggleOtherLanguagesPanel()
    {
        ShowOtherLanguagesPanel = !ShowOtherLanguagesPanel;
    }

    [RelayCommand]
    private void SetThemeLight()
    {
        CurrentThemeMode = AppThemeMode.Light;
    }

    [RelayCommand]
    private void SetThemeDark()
    {
        CurrentThemeMode = AppThemeMode.Dark;
    }

    [RelayCommand]
    private void SetThemeSystem()
    {
        CurrentThemeMode = AppThemeMode.System;
    }

    [RelayCommand]
    private void OpenSettings()
    {
        var dialog = new SettingsDialog(StringKeyTemplate);
        dialog.Owner = Application.Current.MainWindow;
        if (dialog.ShowDialog() == true)
        {
            StringKeyTemplate = dialog.StringKeyTemplate;
            TriggerAutoSave();
        }
    }

    [RelayCommand]
    private void NavigateToSearchResult(ResourceNode? node)
    {
        if (node == null)
            return;

        // 先清除之前的选择状态
        ClearSelectionRecursive(TreeNodes);

        // 确定要在树中选中的节点（如果是叶子节点，选中其父节点）
        ResourceNode? treeNodeToSelect;
        if (node.IsLeaf && node.Parent != null)
        {
            treeNodeToSelect = node.Parent;
        }
        else
        {
            treeNodeToSelect = node;
        }

        // 从目标节点向上展开所有祖先节点
        var ancestorNode = treeNodeToSelect?.Parent;
        while (ancestorNode != null && ancestorNode != _resourceService.RootNode)
        {
            ancestorNode.IsExpanded = true;
            ancestorNode = ancestorNode.Parent;
        }

        // 设置选中状态
        if (treeNodeToSelect != null)
        {
            treeNodeToSelect.IsSelected = true;
            SelectedTreeNode = treeNodeToSelect;
        }

        if (node.IsLeaf)
        {
            SelectedStringItem = node;
        }

        SearchText = string.Empty;
    }

    /// <summary>
    /// 递归清除所有节点的选择状态
    /// </summary>
    private void ClearSelectionRecursive(IEnumerable<ResourceNode> nodes)
    {
        foreach (var node in nodes)
        {
            node.IsSelected = false;
            if (node.Children.Count > 0)
            {
                ClearSelectionRecursive(node.Children);
            }
        }
    }

    /// <summary>
    /// 当字符串值改变时触发自动保存
    /// </summary>
    public void OnStringValueChanged()
    {
        TriggerAutoSave();
    }

    private void TriggerAutoSave()
    {
        _autoSaveTimer?.Stop();
        _autoSaveTimer?.Start();
    }

    public async Task SaveSettingsAsync()
    {
        // 保存资源文件（包含选中状态）
        if (_resourceService.CurrentFilePath != null)
        {
            await SaveFileAsync();
        }
        _settingsService.Save();
    }
}