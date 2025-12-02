using System.Windows;
using CommunityToolkit.Mvvm.Input;

namespace I18nStringEditor.ViewModels;

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