using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Input;
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

/// <summary>
/// 简单输入对话框
/// </summary>
public class InputDialog : Window
{
    public string InputText { get; private set; } = string.Empty;
    private System.Windows.Controls.TextBox _textBox;

    public InputDialog(string title, string prompt)
    {
        Title = title;
        Width = 400;
        Height = 150;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        ResizeMode = ResizeMode.NoResize;
        SizeToContent = SizeToContent.Height;

        var grid = new System.Windows.Controls.Grid();
        grid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = System.Windows.GridLength.Auto });
        grid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = System.Windows.GridLength.Auto });
        grid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = System.Windows.GridLength.Auto });
        grid.Margin = new Thickness(10);

        var label = new System.Windows.Controls.Label { Content = prompt };
        System.Windows.Controls.Grid.SetRow(label, 0);
        grid.Children.Add(label);

        _textBox = new System.Windows.Controls.TextBox { Margin = new Thickness(0, 5, 0, 10) };
        System.Windows.Controls.Grid.SetRow(_textBox, 1);
        grid.Children.Add(_textBox);

        var buttonPanel = new System.Windows.Controls.StackPanel
        {
            Orientation = System.Windows.Controls.Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right
        };
        System.Windows.Controls.Grid.SetRow(buttonPanel, 2);

        var okButton = new System.Windows.Controls.Button { Content = "确定", Width = 75, Margin = new Thickness(0, 0, 10, 0) };
        okButton.Click += (s, e) =>
        {
            InputText = _textBox.Text;
            DialogResult = true;
        };
        buttonPanel.Children.Add(okButton);

        var cancelButton = new System.Windows.Controls.Button { Content = "取消", Width = 75 };
        cancelButton.Click += (s, e) => DialogResult = false;
        buttonPanel.Children.Add(cancelButton);

        grid.Children.Add(buttonPanel);
        Content = grid;

        // 设置Alt+S快捷键保存
        var saveBinding = new System.Windows.Input.KeyBinding(
            new RelayCommand(() => { InputText = _textBox.Text; DialogResult = true; }),
            System.Windows.Input.Key.S,
            System.Windows.Input.ModifierKeys.Alt);
        InputBindings.Add(saveBinding);

        // 窗口加载后将焦点设置到输入框
        Loaded += (s, e) => _textBox.Focus();
    }
}

/// <summary>
/// 添加字符串对话框
/// </summary>
public class AddStringDialog : Window
{
    public string StringKey { get; private set; } = string.Empty;
    public string StringValue { get; private set; } = string.Empty;
    public string StringComment { get; private set; } = string.Empty;

    private System.Windows.Controls.TextBox _keyTextBox;
    private System.Windows.Controls.TextBox _valueTextBox;
    private System.Windows.Controls.TextBox _commentTextBox;

    public AddStringDialog()
    {
        Title = "添加字符串";
        Width = 450;
        Height = 250;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        ResizeMode = ResizeMode.NoResize;
        SizeToContent = SizeToContent.Height;

        var grid = new System.Windows.Controls.Grid();
        grid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = System.Windows.GridLength.Auto });
        grid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = System.Windows.GridLength.Auto });
        grid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = System.Windows.GridLength.Auto });
        grid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = System.Windows.GridLength.Auto });
        grid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = System.Windows.GridLength.Auto });
        grid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = System.Windows.GridLength.Auto });
        grid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = System.Windows.GridLength.Auto });
        grid.Margin = new Thickness(10);

        var keyLabel = new System.Windows.Controls.Label { Content = "Key:" };
        System.Windows.Controls.Grid.SetRow(keyLabel, 0);
        grid.Children.Add(keyLabel);

        _keyTextBox = new System.Windows.Controls.TextBox { Margin = new Thickness(0, 0, 0, 5) };
        System.Windows.Controls.Grid.SetRow(_keyTextBox, 1);
        grid.Children.Add(_keyTextBox);

        var valueLabel = new System.Windows.Controls.Label { Content = "值:" };
        System.Windows.Controls.Grid.SetRow(valueLabel, 2);
        grid.Children.Add(valueLabel);

        _valueTextBox = new System.Windows.Controls.TextBox { Margin = new Thickness(0, 0, 0, 5) };
        System.Windows.Controls.Grid.SetRow(_valueTextBox, 3);
        grid.Children.Add(_valueTextBox);

        var commentLabel = new System.Windows.Controls.Label { Content = "说明:" };
        System.Windows.Controls.Grid.SetRow(commentLabel, 4);
        grid.Children.Add(commentLabel);

        _commentTextBox = new System.Windows.Controls.TextBox { Margin = new Thickness(0, 0, 0, 10) };
        System.Windows.Controls.Grid.SetRow(_commentTextBox, 5);
        grid.Children.Add(_commentTextBox);

        var buttonPanel = new System.Windows.Controls.StackPanel
        {
            Orientation = System.Windows.Controls.Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right
        };
        System.Windows.Controls.Grid.SetRow(buttonPanel, 6);

        var okButton = new System.Windows.Controls.Button { Content = "确定(_S)", Width = 75, Margin = new Thickness(0, 0, 10, 0) };
        okButton.Click += (s, e) =>
        {
            if (string.IsNullOrWhiteSpace(_keyTextBox.Text))
            {
                MessageBox.Show("Key 不能为空", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            StringKey = _keyTextBox.Text;
            StringValue = _valueTextBox.Text;
            StringComment = _commentTextBox.Text;
            DialogResult = true;
        };
        buttonPanel.Children.Add(okButton);

        var cancelButton = new System.Windows.Controls.Button { Content = "取消", Width = 75 };
        cancelButton.Click += (s, e) => DialogResult = false;
        buttonPanel.Children.Add(cancelButton);

        grid.Children.Add(buttonPanel);
        Content = grid;

        // 设置Alt+S快捷键保存
        var saveBinding = new System.Windows.Input.KeyBinding(
            new RelayCommand(() =>
            {
                if (string.IsNullOrWhiteSpace(_keyTextBox.Text))
                {
                    MessageBox.Show("Key 不能为空", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                StringKey = _keyTextBox.Text;
                StringValue = _valueTextBox.Text;
                StringComment = _commentTextBox.Text;
                DialogResult = true;
            }),
            System.Windows.Input.Key.S,
            System.Windows.Input.ModifierKeys.Alt);
        InputBindings.Add(saveBinding);

        // 窗口加载后将焦点设置到第一个输入框，并检查剪贴板内容
        Loaded += (s, e) =>
        {
            // 如果剪贴板中有文本，自动填写到"值"字段
            if (Clipboard.ContainsText())
            {
                var clipboardText = Clipboard.GetText();
                if (!string.IsNullOrEmpty(clipboardText))
                {
                    _valueTextBox.Text = clipboardText;
                }
            }
            _keyTextBox.Focus();
        };
    }
}

/// <summary>
/// 设置对话框
/// </summary>
public class SettingsDialog : Window
{
    public string StringKeyTemplate { get; private set; }
    public bool EnableGlobalHotkey { get; private set; }
    public string GlobalHotkey { get; private set; }

    private System.Windows.Controls.TextBox _templateTextBox;
    private System.Windows.Controls.CheckBox _enableHotkeyCheckBox;
    private System.Windows.Controls.TextBox _hotkeyTextBox;
    private ModifierKeys _recordedModifiers = ModifierKeys.None;
    private Key _recordedKey = Key.None;
    private bool _isRecording = false;

    public SettingsDialog(string currentTemplate)
    {
        StringKeyTemplate = currentTemplate;
        
        // 读取当前快捷键设置
        EnableGlobalHotkey = App.SettingsService?.Settings.EnableGlobalHotkey ?? true;
        GlobalHotkey = App.SettingsService?.Settings.GlobalHotkey ?? "Ctrl + Alt + I";
        
        Title = "设置";
        Width = 500;
        Height = 320;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        ResizeMode = ResizeMode.NoResize;
        SizeToContent = SizeToContent.Height;

        var mainGrid = new System.Windows.Controls.Grid();
        mainGrid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = System.Windows.GridLength.Auto });
        mainGrid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = System.Windows.GridLength.Auto });
        mainGrid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = System.Windows.GridLength.Auto });
        mainGrid.Margin = new Thickness(10);

        // StringKey 模板设置
        var templateGroup = CreateGroupBox("StringKey 模板");
        var templateGrid = new System.Windows.Controls.Grid();
        templateGrid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = System.Windows.GridLength.Auto });
        templateGrid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = System.Windows.GridLength.Auto });
        templateGrid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = System.Windows.GridLength.Auto });

        var templateLabel = new System.Windows.Controls.Label { Content = "模板 (使用 {KEY} 作为占位符):" };
        System.Windows.Controls.Grid.SetRow(templateLabel, 0);
        templateGrid.Children.Add(templateLabel);

        _templateTextBox = new System.Windows.Controls.TextBox
        {
            Text = currentTemplate,
            Margin = new Thickness(0, 0, 0, 5)
        };
        System.Windows.Controls.Grid.SetRow(_templateTextBox, 1);
        templateGrid.Children.Add(_templateTextBox);

        var hintLabel = new System.Windows.Controls.Label
        {
            Content = "示例: {I18N {x:Static Strings.{KEY}}}",
            Foreground = System.Windows.Media.Brushes.Gray,
            FontSize = 11
        };
        System.Windows.Controls.Grid.SetRow(hintLabel, 2);
        templateGrid.Children.Add(hintLabel);

        templateGroup.Content = templateGrid;
        System.Windows.Controls.Grid.SetRow(templateGroup, 0);
        mainGrid.Children.Add(templateGroup);

        // 快捷键设置
        var hotkeyGroup = CreateGroupBox("全局快捷键");
        var hotkeyGrid = new System.Windows.Controls.Grid();
        hotkeyGrid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = System.Windows.GridLength.Auto });
        hotkeyGrid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = System.Windows.GridLength.Auto });
        hotkeyGrid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = System.Windows.GridLength.Auto });

        _enableHotkeyCheckBox = new System.Windows.Controls.CheckBox
        {
            Content = "启用全局快捷键（唤起主窗口）",
            IsChecked = EnableGlobalHotkey,
            Margin = new Thickness(0, 0, 0, 5)
        };
        _enableHotkeyCheckBox.Checked += (s, e) => { if (_hotkeyTextBox != null) _hotkeyTextBox.IsEnabled = true; };
        _enableHotkeyCheckBox.Unchecked += (s, e) => { if (_hotkeyTextBox != null) _hotkeyTextBox.IsEnabled = false; };
        System.Windows.Controls.Grid.SetRow(_enableHotkeyCheckBox, 0);
        hotkeyGrid.Children.Add(_enableHotkeyCheckBox);

        var hotkeyPanel = new System.Windows.Controls.StackPanel { Orientation = System.Windows.Controls.Orientation.Horizontal };
        var hotkeyLabel = new System.Windows.Controls.Label { Content = "快捷键:", VerticalAlignment = VerticalAlignment.Center };
        hotkeyPanel.Children.Add(hotkeyLabel);

        _hotkeyTextBox = new System.Windows.Controls.TextBox
        {
            Text = GlobalHotkey,
            Width = 200,
            Margin = new Thickness(5, 0, 10, 0),
            IsReadOnly = true,
            IsEnabled = EnableGlobalHotkey,
            VerticalAlignment = VerticalAlignment.Center
        };
        _hotkeyTextBox.PreviewKeyDown += HotkeyTextBox_PreviewKeyDown;
        _hotkeyTextBox.PreviewKeyUp += HotkeyTextBox_PreviewKeyUp;
        _hotkeyTextBox.GotFocus += (s, e) => { _isRecording = true; _hotkeyTextBox.Text = "按下快捷键..."; };
        _hotkeyTextBox.LostFocus += (s, e) => 
        { 
            _isRecording = false;
            if (_hotkeyTextBox.Text == "按下快捷键...")
            {
                _hotkeyTextBox.Text = GlobalHotkey;
            }
        };
        hotkeyPanel.Children.Add(_hotkeyTextBox);

        var clearButton = new System.Windows.Controls.Button { Content = "清除", Width = 50 };
        clearButton.Click += (s, e) => 
        {
            _recordedModifiers = ModifierKeys.None;
            _recordedKey = Key.None;
            GlobalHotkey = "无";
            _hotkeyTextBox.Text = GlobalHotkey;
        };
        hotkeyPanel.Children.Add(clearButton);

        System.Windows.Controls.Grid.SetRow(hotkeyPanel, 1);
        hotkeyGrid.Children.Add(hotkeyPanel);

        var hotkeyHint = new System.Windows.Controls.Label
        {
            Content = "点击输入框后按下想要的快捷键组合（建议使用 Ctrl+Alt 组合）",
            Foreground = System.Windows.Media.Brushes.Gray,
            FontSize = 11,
            Margin = new Thickness(0, 5, 0, 0)
        };
        System.Windows.Controls.Grid.SetRow(hotkeyHint, 2);
        hotkeyGrid.Children.Add(hotkeyHint);

        hotkeyGroup.Content = hotkeyGrid;
        System.Windows.Controls.Grid.SetRow(hotkeyGroup, 1);
        mainGrid.Children.Add(hotkeyGroup);

        // 按钮区域
        var buttonPanel = new System.Windows.Controls.StackPanel
        {
            Orientation = System.Windows.Controls.Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(0, 15, 0, 0)
        };
        System.Windows.Controls.Grid.SetRow(buttonPanel, 2);

        var okButton = new System.Windows.Controls.Button { Content = "确定(_S)", Width = 75, Margin = new Thickness(0, 0, 10, 0) };
        okButton.Click += (s, e) =>
        {
            StringKeyTemplate = _templateTextBox.Text;
            EnableGlobalHotkey = _enableHotkeyCheckBox.IsChecked ?? false;
            
            // 更新快捷键设置
            App.UpdateHotkeySettings(EnableGlobalHotkey, GlobalHotkey);
            
            DialogResult = true;
        };
        buttonPanel.Children.Add(okButton);

        var cancelButton = new System.Windows.Controls.Button { Content = "取消", Width = 75 };
        cancelButton.Click += (s, e) => DialogResult = false;
        buttonPanel.Children.Add(cancelButton);

        mainGrid.Children.Add(buttonPanel);
        Content = mainGrid;

        // 设置Alt+S快捷键保存
        var saveBinding = new System.Windows.Input.KeyBinding(
            new RelayCommand(() => 
            {
                StringKeyTemplate = _templateTextBox.Text;
                EnableGlobalHotkey = _enableHotkeyCheckBox.IsChecked ?? false;
                App.UpdateHotkeySettings(EnableGlobalHotkey, GlobalHotkey);
                DialogResult = true;
            }),
            System.Windows.Input.Key.S,
            System.Windows.Input.ModifierKeys.Alt);
        InputBindings.Add(saveBinding);

        // 窗口加载后将焦点设置到输入框
        Loaded += (s, e) => _templateTextBox.Focus();
    }

    private System.Windows.Controls.GroupBox CreateGroupBox(string header)
    {
        return new System.Windows.Controls.GroupBox
        {
            Header = header,
            Margin = new Thickness(0, 0, 0, 10),
            Padding = new Thickness(10)
        };
    }

    private void HotkeyTextBox_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (!_isRecording)
            return;

        e.Handled = true;

        // 获取修饰键
        _recordedModifiers = Keyboard.Modifiers;

        // 获取主键（忽略单独的修饰键）
        var key = e.Key == Key.System ? e.SystemKey : e.Key;
        if (key != Key.LeftCtrl && key != Key.RightCtrl &&
            key != Key.LeftAlt && key != Key.RightAlt &&
            key != Key.LeftShift && key != Key.RightShift &&
            key != Key.LWin && key != Key.RWin)
        {
            _recordedKey = key;

            // 更新显示
            if (_recordedKey != Key.None)
            {
                GlobalHotkey = I18nStringEditor.Services.GlobalHotkeyService.GetHotkeyDisplayString(_recordedModifiers, _recordedKey);
                _hotkeyTextBox.Text = GlobalHotkey;
            }
        }
        else
        {
            // 只按下修饰键时显示当前状态
            _hotkeyTextBox.Text = GetModifiersString(_recordedModifiers) + " + ...";
        }
    }

    private void HotkeyTextBox_PreviewKeyUp(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (!_isRecording)
            return;

        // 如果已经录制了完整的快捷键，结束录制
        if (_recordedKey != Key.None)
        {
            _isRecording = false;
            Keyboard.ClearFocus();
        }
    }

    private string GetModifiersString(ModifierKeys modifiers)
    {
        var parts = new List<string>();
        if (modifiers.HasFlag(ModifierKeys.Control))
            parts.Add("Ctrl");
        if (modifiers.HasFlag(ModifierKeys.Alt))
            parts.Add("Alt");
        if (modifiers.HasFlag(ModifierKeys.Shift))
            parts.Add("Shift");
        if (modifiers.HasFlag(ModifierKeys.Windows))
            parts.Add("Win");
        return string.Join(" + ", parts);
    }
}
