using System.Windows;
using CommunityToolkit.Mvvm.Input;

namespace I18nStringEditor.ViewModels;

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