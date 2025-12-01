using System.Collections.ObjectModel;
using System.IO;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using I18nStringEditor.Models;

namespace I18nStringEditor.Services;

/// <summary>
/// 资源文件服务
/// </summary>
public class ResourceFileService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    /// <summary>
    /// 当前加载的文件路径
    /// </summary>
    public string? CurrentFilePath { get; private set; }

    /// <summary>
    /// 当前资源的附加信息
    /// </summary>
    public ResourceInfo? CurrentInfo { get; private set; }

    /// <summary>
    /// 根节点
    /// </summary>
    public ResourceNode? RootNode { get; private set; }

    /// <summary>
    /// 加载资源文件
    /// </summary>
    public async Task<ResourceNode?> LoadAsync(string filePath)
    {
        if (!File.Exists(filePath))
            return null;

        CurrentFilePath = filePath;
        var json = await File.ReadAllTextAsync(filePath);
        var jsonNode = JsonNode.Parse(json);

        if (jsonNode == null)
            return null;

        RootNode = new ResourceNode("Root");
        ParseJsonToNode(jsonNode, RootNode);

        // 加载info文件
        await LoadInfoAsync();

        // 应用展开状态和注释
        ApplyInfoToNodes(RootNode);

        return RootNode;
    }

    /// <summary>
    /// 保存资源文件
    /// </summary>
    public async Task SaveAsync()
    {
        if (string.IsNullOrEmpty(CurrentFilePath) || RootNode == null)
            return;

        var jsonObject = NodeToJson(RootNode);
        var json = JsonSerializer.Serialize(jsonObject, JsonOptions);
        await File.WriteAllTextAsync(CurrentFilePath, json);

        // 保存info文件
        await SaveInfoAsync();
    }

    /// <summary>
    /// 获取同目录下的其他语言文件
    /// </summary>
    public List<string> GetOtherLanguageFiles()
    {
        if (string.IsNullOrEmpty(CurrentFilePath))
            return new List<string>();

        var directory = Path.GetDirectoryName(CurrentFilePath);
        if (string.IsNullOrEmpty(directory))
            return new List<string>();

        var currentFileName = Path.GetFileName(CurrentFilePath);
        return Directory.GetFiles(directory, "*.json")
            .Where(f => !Path.GetFileName(f).Equals(currentFileName, StringComparison.OrdinalIgnoreCase))
            .Where(f => !f.EndsWith(".info", StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    /// <summary>
    /// 获取其他语言文件中指定键的值
    /// </summary>
    public async Task<List<OtherLanguageValue>> GetOtherLanguageValuesAsync(string fullPath)
    {
        var result = new List<OtherLanguageValue>();
        var otherFiles = GetOtherLanguageFiles();

        foreach (var file in otherFiles)
        {
            try
            {
                var json = await File.ReadAllTextAsync(file);
                var jsonNode = JsonNode.Parse(json);
                var value = GetValueByPath(jsonNode, fullPath);

                result.Add(new OtherLanguageValue
                {
                    LanguageFile = Path.GetFileName(file),
                    Value = value
                });
            }
            catch
            {
                result.Add(new OtherLanguageValue
                {
                    LanguageFile = Path.GetFileName(file),
                    Value = "[读取失败]"
                });
            }
        }

        return result;
    }

    private string? GetValueByPath(JsonNode? node, string path)
    {
        if (node == null)
            return null;

        var parts = path.Split('.');
        JsonNode? current = node;

        foreach (var part in parts)
        {
            if (current is JsonObject obj && obj.ContainsKey(part))
            {
                current = obj[part];
            }
            else
            {
                return null;
            }
        }

        return current?.GetValue<string>();
    }

    private void ParseJsonToNode(JsonNode? node, ResourceNode parentNode)
    {
        if (node is not JsonObject jsonObject)
            return;

        foreach (var kvp in jsonObject)
        {
            var childNode = new ResourceNode(kvp.Key);
            parentNode.AddChild(childNode);

            if (kvp.Value is JsonObject)
            {
                ParseJsonToNode(kvp.Value, childNode);
            }
            else if (kvp.Value is JsonValue jsonValue)
            {
                childNode.Value = jsonValue.ToString();
            }
        }
    }

    private JsonObject NodeToJson(ResourceNode node)
    {
        var jsonObject = new JsonObject();

        foreach (var child in node.Children)
        {
            if (child.IsLeaf)
            {
                jsonObject[child.Key] = child.Value;
            }
            else
            {
                jsonObject[child.Key] = NodeToJson(child);
            }
        }

        return jsonObject;
    }

    private string GetInfoFilePath()
    {
        if (string.IsNullOrEmpty(CurrentFilePath))
            return string.Empty;

        return CurrentFilePath + ".info";
    }

    private async Task LoadInfoAsync()
    {
        var infoPath = GetInfoFilePath();
        if (File.Exists(infoPath))
        {
            try
            {
                var json = await File.ReadAllTextAsync(infoPath);
                CurrentInfo = JsonSerializer.Deserialize<ResourceInfo>(json) ?? new ResourceInfo();
            }
            catch
            {
                CurrentInfo = new ResourceInfo();
            }
        }
        else
        {
            CurrentInfo = new ResourceInfo();
        }
    }

    private async Task SaveInfoAsync()
    {
        if (RootNode == null || CurrentInfo == null)
            return;

        // 收集所有节点的展开状态和注释
        CollectNodeInfo(RootNode);

        var infoPath = GetInfoFilePath();
        var json = JsonSerializer.Serialize(CurrentInfo, JsonOptions);
        await File.WriteAllTextAsync(infoPath, json);
    }

    private void CollectNodeInfo(ResourceNode node)
    {
        foreach (var child in node.Children)
        {
            if (!child.IsLeaf)
            {
                CurrentInfo!.ExpandedStates[child.FullPath] = child.IsExpanded;
            }

            if (!string.IsNullOrEmpty(child.Comment))
            {
                CurrentInfo!.Comments[child.FullPath] = child.Comment;
            }
            else
            {
                CurrentInfo!.Comments.Remove(child.FullPath);
            }

            CollectNodeInfo(child);
        }
    }

    private void ApplyInfoToNodes(ResourceNode node)
    {
        if (CurrentInfo == null)
            return;

        foreach (var child in node.Children)
        {
            if (CurrentInfo.ExpandedStates.TryGetValue(child.FullPath, out var isExpanded))
            {
                child.IsExpanded = isExpanded;
            }

            if (CurrentInfo.Comments.TryGetValue(child.FullPath, out var comment))
            {
                child.Comment = comment;
            }

            ApplyInfoToNodes(child);
        }
    }

    /// <summary>
    /// 创建新节点
    /// </summary>
    public ResourceNode CreateNode(ResourceNode parent, string key, string? value = null)
    {
        var node = new ResourceNode(key, value);
        parent.AddChild(node);
        return node;
    }

    /// <summary>
    /// 删除节点
    /// </summary>
    public void DeleteNode(ResourceNode node)
    {
        node.Parent?.RemoveChild(node);
    }

    /// <summary>
    /// 搜索节点
    /// </summary>
    public List<ResourceNode> Search(string keyword)
    {
        var result = new List<ResourceNode>();
        if (RootNode == null || string.IsNullOrWhiteSpace(keyword))
            return result;

        SearchInNode(RootNode, keyword.ToLower(), result);
        return result;
    }

    private void SearchInNode(ResourceNode node, string keyword, List<ResourceNode> result)
    {
        foreach (var child in node.Children)
        {
            if (child.Key.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                (child.Value?.Contains(keyword, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (child.Comment?.Contains(keyword, StringComparison.OrdinalIgnoreCase) ?? false))
            {
                result.Add(child);
            }

            SearchInNode(child, keyword, result);
        }
    }
}
