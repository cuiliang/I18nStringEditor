using System.Windows;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;

namespace I18nStringEditor.ViewModels;

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