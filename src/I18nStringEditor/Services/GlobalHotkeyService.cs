using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;

namespace I18nStringEditor.Services;

/// <summary>
/// 全局快捷键服务
/// </summary>
public class GlobalHotkeyService : IDisposable
{
    #region Win32 API

    [DllImport("user32.dll")]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll")]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    private const int WM_HOTKEY = 0x0312;

    // 修饰键常量
    private const uint MOD_NONE = 0x0000;
    private const uint MOD_ALT = 0x0001;
    private const uint MOD_CONTROL = 0x0002;
    private const uint MOD_SHIFT = 0x0004;
    private const uint MOD_WIN = 0x0008;
    private const uint MOD_NOREPEAT = 0x4000;

    #endregion

    private IntPtr _windowHandle;
    private HwndSource? _source;
    private readonly Dictionary<int, Action> _hotkeyActions = new();
    private int _currentId = 0;
    private bool _isDisposed;

    /// <summary>
    /// 快捷键触发事件
    /// </summary>
    public event EventHandler? HotkeyTriggered;

    /// <summary>
    /// 初始化全局快捷键服务
    /// </summary>
    /// <param name="window">主窗口</param>
    public void Initialize(Window window)
    {
        var helper = new WindowInteropHelper(window);
        
        // 确保窗口句柄已创建
        if (helper.Handle == IntPtr.Zero)
        {
            helper.EnsureHandle();
        }
        
        _windowHandle = helper.Handle;
        _source = HwndSource.FromHwnd(_windowHandle);
        _source?.AddHook(HwndHook);
    }

    /// <summary>
    /// 注册全局快捷键
    /// </summary>
    /// <param name="modifiers">修饰键</param>
    /// <param name="key">主键</param>
    /// <param name="callback">回调方法</param>
    /// <returns>注册的快捷键ID，失败返回-1</returns>
    public int RegisterHotkey(ModifierKeys modifiers, Key key, Action callback)
    {
        if (_windowHandle == IntPtr.Zero)
            return -1;

        uint mod = MOD_NOREPEAT; // 防止重复触发
        if (modifiers.HasFlag(ModifierKeys.Alt))
            mod |= MOD_ALT;
        if (modifiers.HasFlag(ModifierKeys.Control))
            mod |= MOD_CONTROL;
        if (modifiers.HasFlag(ModifierKeys.Shift))
            mod |= MOD_SHIFT;
        if (modifiers.HasFlag(ModifierKeys.Windows))
            mod |= MOD_WIN;

        uint vk = (uint)KeyInterop.VirtualKeyFromKey(key);

        int id = ++_currentId;
        if (RegisterHotKey(_windowHandle, id, mod, vk))
        {
            _hotkeyActions[id] = callback;
            return id;
        }

        return -1;
    }

    /// <summary>
    /// 注销全局快捷键
    /// </summary>
    /// <param name="id">快捷键ID</param>
    public void UnregisterHotkey(int id)
    {
        if (_windowHandle != IntPtr.Zero && id > 0)
        {
            UnregisterHotKey(_windowHandle, id);
            _hotkeyActions.Remove(id);
        }
    }

    /// <summary>
    /// 注销所有快捷键
    /// </summary>
    public void UnregisterAllHotkeys()
    {
        foreach (var id in _hotkeyActions.Keys.ToList())
        {
            UnregisterHotkey(id);
        }
    }

    private IntPtr HwndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WM_HOTKEY)
        {
            int id = wParam.ToInt32();
            if (_hotkeyActions.TryGetValue(id, out var action))
            {
                action?.Invoke();
                HotkeyTriggered?.Invoke(this, EventArgs.Empty);
                handled = true;
            }
        }
        return IntPtr.Zero;
    }

    public void Dispose()
    {
        if (_isDisposed)
            return;

        UnregisterAllHotkeys();
        _source?.RemoveHook(HwndHook);
        _source = null;
        _isDisposed = true;
    }

    #region 静态辅助方法

    /// <summary>
    /// 将 ModifierKeys 和 Key 转换为显示字符串
    /// </summary>
    public static string GetHotkeyDisplayString(ModifierKeys modifiers, Key key)
    {
        if (key == Key.None)
            return "无";

        var parts = new List<string>();

        if (modifiers.HasFlag(ModifierKeys.Control))
            parts.Add("Ctrl");
        if (modifiers.HasFlag(ModifierKeys.Alt))
            parts.Add("Alt");
        if (modifiers.HasFlag(ModifierKeys.Shift))
            parts.Add("Shift");
        if (modifiers.HasFlag(ModifierKeys.Windows))
            parts.Add("Win");

        parts.Add(GetKeyDisplayName(key));

        return string.Join(" + ", parts);
    }

    /// <summary>
    /// 获取按键的显示名称
    /// </summary>
    private static string GetKeyDisplayName(Key key)
    {
        return key switch
        {
            Key.D0 => "0",
            Key.D1 => "1",
            Key.D2 => "2",
            Key.D3 => "3",
            Key.D4 => "4",
            Key.D5 => "5",
            Key.D6 => "6",
            Key.D7 => "7",
            Key.D8 => "8",
            Key.D9 => "9",
            Key.OemTilde => "~",
            Key.OemMinus => "-",
            Key.OemPlus => "=",
            Key.OemOpenBrackets => "[",
            Key.OemCloseBrackets => "]",
            Key.OemPipe => "\\",
            Key.OemSemicolon => ";",
            Key.OemQuotes => "'",
            Key.OemComma => ",",
            Key.OemPeriod => ".",
            Key.OemQuestion => "/",
            _ => key.ToString()
        };
    }

    /// <summary>
    /// 解析快捷键字符串为 ModifierKeys 和 Key
    /// </summary>
    public static (ModifierKeys modifiers, Key key) ParseHotkeyString(string hotkeyString)
    {
        if (string.IsNullOrWhiteSpace(hotkeyString) || hotkeyString == "无")
            return (ModifierKeys.None, Key.None);

        var modifiers = ModifierKeys.None;
        var key = Key.None;

        var parts = hotkeyString.Split(new[] { '+', ' ' }, StringSplitOptions.RemoveEmptyEntries);
        
        foreach (var part in parts)
        {
            var trimmedPart = part.Trim();
            switch (trimmedPart.ToLower())
            {
                case "ctrl":
                case "control":
                    modifiers |= ModifierKeys.Control;
                    break;
                case "alt":
                    modifiers |= ModifierKeys.Alt;
                    break;
                case "shift":
                    modifiers |= ModifierKeys.Shift;
                    break;
                case "win":
                case "windows":
                    modifiers |= ModifierKeys.Windows;
                    break;
                default:
                    // 尝试解析为 Key
                    key = ParseKey(trimmedPart);
                    break;
            }
        }

        return (modifiers, key);
    }

    /// <summary>
    /// 解析按键字符串
    /// </summary>
    private static Key ParseKey(string keyString)
    {
        // 处理数字键
        if (keyString.Length == 1 && char.IsDigit(keyString[0]))
        {
            return keyString[0] switch
            {
                '0' => Key.D0,
                '1' => Key.D1,
                '2' => Key.D2,
                '3' => Key.D3,
                '4' => Key.D4,
                '5' => Key.D5,
                '6' => Key.D6,
                '7' => Key.D7,
                '8' => Key.D8,
                '9' => Key.D9,
                _ => Key.None
            };
        }

        // 处理字母键
        if (keyString.Length == 1 && char.IsLetter(keyString[0]))
        {
            if (Enum.TryParse<Key>(keyString.ToUpper(), out var letterKey))
                return letterKey;
        }

        // 处理其他键
        if (Enum.TryParse<Key>(keyString, true, out var key))
            return key;

        return Key.None;
    }

    #endregion
}
