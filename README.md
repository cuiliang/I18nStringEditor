# I18nStringEditor

一个基于 .NET 10 + WPF 开发的国际化（i18n）字符串资源文件编辑器，专为管理 JSON 格式的多语言资源文件而设计。

![.NET](https://img.shields.io/badge/.NET-10.0-purple)
![WPF](https://img.shields.io/badge/WPF-Windows-blue)
![License](https://img.shields.io/badge/License-MIT-green)

## ✨ 功能特点

### 📁 资源文件管理
- **JSON 格式支持**：支持层级嵌套的 JSON 资源文件
- **自动保存**：修改后自动保存到资源文件，无需手动操作
- **记忆上次文件**：程序启动时自动打开上次编辑的文件
- **Info 文件**：为每个资源文件生成 `.info` 配套文件，存储节点状态、注释等信息

### 🌳 树形节点管理
- **可视化结构**：左侧树形展示资源文件的层级结构
- **展开/折叠**：支持节点的展开与折叠，状态自动保存
- **添加分组**：快速创建新的资源分组
- **排序功能**：对子节点进行排序整理
- **删除分组**：删除不需要的分组及其所有内容

### ✏️ 字符串编辑
- **表格式编辑**：右侧面板以表格形式展示字符串资源
- **三列显示**：Key、值、说明三列清晰展示
- **直接编辑**：在表格中直接修改内容
- **添加/删除**：快速添加新字符串或删除现有字符串

### 🌐 多语言支持
- **其他语言面板**：显示同目录下其他语言文件中相同 Key 的值
- **面板切换**：可自由显示/隐藏其他语言面板
- **方便对照**：便于翻译时参考其他语言版本

### 🔍 搜索功能
- **全局搜索**：支持在所有资源中搜索关键词
- **实时结果**：输入时实时显示搜索结果
- **快速定位**：点击搜索结果快速跳转到对应位置

### 📋 StringKey 复制
- **模板配置**：自定义 StringKey 模板
- **一键复制**：快速生成并复制用于 XAML 的绑定文本
- **示例格式**：`{I18N {x:Static Strings.Areas_Accounts_Common_LocalAccountDesc}}`

## ⌨️ 快捷键

| 快捷键 | 功能 |
|--------|------|
| `Ctrl + O` | 打开文件 |
| `Ctrl + S` | 保存文件 |
| `Ctrl + N` | 添加字符串 |
| `Ctrl + Shift + N` | 添加分组 |
| `Ctrl + Shift + C` | 复制 StringKey |
| `Delete` | 删除选中的字符串 |
| `F3` | 显示/隐藏其他语言面板 |

## 📁 资源文件格式

支持嵌套层级的 JSON 格式资源文件：

```json
{
  "Common": {
    "Ok": "确定",
    "Cancel": "取消",
    "Error": "错误"
  },
  "MainWindow": {
    "Title": "我的超级应用",
    "Welcome": "欢迎使用",
    "Menu": {
      "File": "文件",
      "Edit": "编辑"
    }
  },
  "Areas": {
    "Accounts": {
      "AccountSelectionWindow": {
        "Title": "选择账户",
        "Prompt": "请选择一个账户以继续："
      }
    }
  }
}
```

## 🚀 快速开始

### 环境要求
- Windows 10/11
- .NET 10.0 Runtime

### 运行程序

1. 克隆仓库
```bash
git clone https://github.com/cuiliang/I18nStringEditor.git
```

2. 进入项目目录
```bash
cd I18nStringEditor/src/I18nStringEditor
```

3. 构建并运行
```bash
dotnet build
dotnet run
```

### 使用步骤

1. **打开资源文件**：点击工具栏"打开"按钮或使用 `Ctrl+O` 选择 JSON 资源文件
2. **浏览结构**：在左侧树形视图中浏览资源文件的层级结构
3. **编辑字符串**：选择一个分组节点，在右侧表格中编辑字符串的 Key、值和说明
4. **添加资源**：使用工具栏按钮或快捷键添加新的分组或字符串
5. **复制 Key**：选中字符串后使用 `Ctrl+Shift+C` 复制用于 XAML 的绑定代码
6. **自动保存**：所有修改会自动保存，无需手动操作

## ⚙️ 设置

点击工具栏的"设置"按钮可以配置：

- **StringKey 模板**：自定义复制 StringKey 时使用的模板格式
  - 使用 `{KEY}` 作为占位符
  - 默认模板：`{I18N {x:Static Strings.{KEY}}}`

## 📂 项目结构

```
I18nStringEditor/
├── Models/              # 数据模型
│   ├── AppSettings.cs       # 应用设置
│   ├── ResourceNode.cs      # 资源节点
│   ├── ResourceInfo.cs      # 资源信息
│   └── OtherLanguageValue.cs# 其他语言值
├── Services/            # 业务服务
│   ├── AppSettingsService.cs    # 设置服务
│   └── ResourceFileService.cs   # 资源文件服务
├── ViewModels/          # 视图模型
│   └── MainViewModel.cs     # 主窗口ViewModel
├── MainWindow.xaml      # 主窗口界面
└── App.xaml             # 应用入口
```

## 🔧 技术栈

- **.NET 10.0**：最新的 .NET 运行时
- **WPF**：Windows Presentation Foundation 桌面框架
- **CommunityToolkit.Mvvm**：MVVM 工具包
- **System.Text.Json**：JSON 序列化/反序列化

## 📝 Info 文件

程序会在资源文件同目录下生成 `资源文件名.info` 文件，用于存储：
- 节点的展开/折叠状态
- 字符串的注释说明
- StringKey 模板等全局设置

## 📄 License

MIT License

## 🤝 贡献

欢迎提交 Issue 和 Pull Request！
