# 本地知识库助手 📚

<div align="center">

![.NET](https://img.shields.io/badge/.NET-7.0-512BD4?style=flat-square&logo=dotnet)
![WPF](https://img.shields.io/badge/WPF-Windows-0078D4?style=flat-square&logo=windows)
![Ollama](https://img.shields.io/badge/Ollama-Local%20AI-00BCD4?style=flat-square)
![License](https://img.shields.io/badge/License-MIT-green?style=flat-square)

基于 **Ollama** 的本地知识库 WPF 应用程序，支持 Word/Excel 文档的智能问答，采用现代化 Material Design 界面。

[功能特点](#功能特点) • [快速开始](#快速开始) • [技术栈](#技术栈) • [项目结构](#项目结构)

</div>

---

## ✨ 功能特点

### 📁 知识库管理
- ✅ **多格式支持** - Word（.docx, .doc）和 Excel（.xlsx, .xls）文档
- ✅ **批量导入** - 支持添加整个文件夹，自动扫描所有文档
- ✅ **智能限制** - 最多 10 个文档，保证查询效率
- ✅ **AI 摘要生成** - 自动提取文档关键内容，生成结构化摘要
- ✅ **摘要缓存** - 删除文档后保留摘要，重新添加无需重复生成
- ✅ **状态可视化** - 红色"生成摘要"按钮 → 绿色"已完成"芯片

### 💬 智能问答
- ✅ **双模式切换** - 知识库模式（文档问答）+ 普通对话模式（纯 AI 聊天）
- ✅ **智能上下文** - 自动筛选相关文档，关键词匹配评分
- ✅ **对话压缩** - 每 3 轮自动压缩历史，保持上下文连贯性
- ✅ **现代 UI** - Material Design 风格，青蓝色渐变，圆角气泡设计
- ✅ **实时反馈** - 加载动画，思考状态显示

### 🎨 界面设计
- ✅ **Material Design** - 暗黑主题，Cyan/Amber 配色
- ✅ **渐变效果** - 聊天气泡、输入框、按钮均采用线性渐变
- ✅ **统一字体** - Microsoft YaHei UI，清晰易读
- ✅ **圆角设计** - 现代化圆角卡片和按钮
- ✅ **醒目开关** - 知识库模式开关，状态一目了然

---

## 🚀 快速开始

### 前置要求

1. **安装 .NET 7.0 SDK**
   ```bash
   winget install Microsoft.DotNet.SDK.7
   ```

2. **安装 Ollama**
   - 下载：https://ollama.ai
   - 安装后运行：
     ```bash
     ollama pull qwen2.5:1.5b
     ```

### 运行应用

1. **克隆仓库**
   ```bash
   git clone https://github.com/SeanWeiSean/Local-Knowledge-Base.git
   cd Local-Knowledge-Base
   ```

2. **构建项目**
   ```bash
   dotnet restore
   dotnet build
   ```

3. **启动应用**
   ```bash
   dotnet run
   ```

### 使用步骤

1. **添加文档** - 点击左侧"📁 添加文件"或"📂 添加文件夹"
2. **生成摘要** - 点击文档卡片上的"生成摘要"按钮（红色芯片）
3. **开启知识库** - 确保右上角"知识库模式"开关已开启（青色边框）
4. **开始提问** - 在底部输入框输入问题，AI 会基于文档内容回答

---

## 🛠 技术栈

- **.NET 7.0** - 目标框架
- **WPF** - Windows Presentation Foundation UI 框架
- **MVVM** - Model-View-ViewModel 架构模式
- **Material Design** - MaterialDesignThemes.Wpf UI 库
- **DocumentFormat.OpenXml** - Word 文档处理（.docx）
- **EPPlus** - Excel 文档处理（.xlsx）
- **Newtonsoft.Json** - JSON 序列化
- **Ollama** - 本地大语言模型服务（qwen2.5:1.5b）

---

## 📂 项目结构

```
LocalKnowledgeBase/
├── Models/                           # 数据模型
│   ├── DocumentItem.cs              # 文档项模型（支持 INotifyPropertyChanged）
│   ├── DocumentType.cs              # 文档类型枚举
│   ├── ChatMessage.cs               # 聊天消息模型
│   └── ChatCompletionModels.cs      # Ollama API 请求/响应模型
├── ViewModels/                       # MVVM 视图模型
│   ├── ViewModelBase.cs             # ViewModel 基类
│   ├── RelayCommand.cs              # ICommand 实现
│   └── MainViewModel.cs             # 主窗口逻辑（文档管理、智能对话）
├── Services/                         # 业务服务层
│   ├── IDocumentService.cs          # 文档服务接口
│   ├── DocumentService.cs           # 文档文本提取服务
│   ├── IChatService.cs              # 聊天服务接口
│   ├── ChatService.cs               # Ollama API 调用服务
│   └── SummaryCacheService.cs       # 摘要缓存持久化服务
├── Converters/                       # XAML 值转换器
│   └── BoolToVisibilityConverter.cs # 布尔到可见性转换
├── MainWindow.xaml                   # 主窗口 UI（Material Design）
├── MainWindow.xaml.cs               # 主窗口代码隐藏
├── App.xaml                          # 应用程序资源（主题配置）
├── App.xaml.cs                       # 应用程序入口
└── LocalKnowledgeBase.csproj         # 项目文件
```

---

## 🎯 核心特性详解

### 智能上下文管理

**文档关键词匹配**
- 提取问题中的关键词，计算文档相关性得分
- 仅将相关文档的摘要作为上下文，提高回答准确度

**对话历史压缩**
- 每 3 轮对话自动触发历史压缩
- 使用 AI 提炼对话要点，保留关键信息
- 防止上下文过长导致 token 超限

### 摘要缓存系统

摘要数据保存在：
```
%LocalAppData%\LocalKnowledgeBase\summary_cache.json
```

**缓存内容包括：**
- 文档完整路径
- AI 生成的摘要文本
- 摘要文件路径
- 生成时间戳

### 双模式对话

| 模式 | 描述 | 上下文来源 |
|------|------|-----------|
| **知识库模式** | 基于文档内容回答 | 相关文档摘要 + 压缩历史 |
| **普通对话** | 纯 AI 聊天 | 仅压缩历史 |

---

## ⚙️ 配置说明

### Ollama 配置

默认端点：`http://localhost:11434/api/chat`  
默认模型：`qwen2.5:1.5b`

可在 [ChatService.cs](Services/ChatService.cs) 中修改：
```csharp
private readonly string _ollamaEndpoint = "http://localhost:11434/api/chat";
private readonly string _modelName = "qwen2.5:1.5b";
```

### 推荐模型

| 模型 | 大小 | 适用场景 |
|------|------|---------|
| qwen2.5:1.5b | ~1GB | 轻量级，速度快，中文友好 ✅ |
| llama3.2:3b | ~2GB | 平衡性能和速度 |
| qwen2.5:7b | ~4.7GB | 高质量回答，需要更多资源 |

---

## 🤝 贡献指南

欢迎提交 Issue 和 Pull Request！

### 开发环境设置

1. Fork 本仓库
2. 创建特性分支：`git checkout -b feature/AmazingFeature`
3. 提交更改：`git commit -m 'Add some AmazingFeature'`
4. 推送到分支：`git push origin feature/AmazingFeature`
5. 提交 Pull Request

---

## 📄 许可证

本项目采用 MIT 许可证 - 详见 [LICENSE](LICENSE) 文件

---

## 🙏 致谢

- [Ollama](https://ollama.ai) - 本地大模型运行时
- [MaterialDesignInXamlToolkit](https://github.com/MaterialDesignInXAML/MaterialDesignInXamlToolkit) - Material Design UI 组件
- [DocumentFormat.OpenXml](https://github.com/OfficeDev/Open-XML-SDK) - Office 文档处理
- [EPPlus](https://github.com/EPPlusSoftware/EPPlus) - Excel 文件处理

---

<div align="center">

**⭐ 如果这个项目对你有帮助，请给个 Star！⭐**

Made with ❤️ by SeanWeiSean

</div>
  "model": "llama2",
  "created_at": "2024-01-09T...",
  "message": {
    "role": "assistant",
    "content": "AI回复内容"
  },
  "done": true
}
```

## 依赖包

- `DocumentFormat.OpenXml` (3.0.0) - 处理 Word 文档
- `EPPlus` (7.0.0) - 处理 Excel 文档
- `Newtonsoft.Json` (13.0.3) - JSON 处理

## 注意事项

1. **EPPlus 许可证**：本项目使用 EPPlus 的非商业许可证。如用于商业用途，请购买商业许可证。
2. **Ollama 服务**：确保 Ollama 服务在后台运行，默认端口为 11434。
3. **文档大小**：为避免超过模型上下文限制，文档提取的文本会被限制在 10000 字符以内。
4. **模型选择**：可以在 `App.config` 中更换不同的 Ollama 模型（如 llama2, mistral, qwen 等）。

## 扩展功能（待实现）

- [ ] 支持更多文档格式（PDF、TXT等）
- [ ] 向量数据库集成，实现更精确的语义检索
- [ ] 对话历史记录
- [ ] 导出对话内容
- [ ] 批量处理文档
- [ ] 自定义提示词模板

## 许可证

MIT License

## 贡献

欢迎提交 Issue 和 Pull Request！
