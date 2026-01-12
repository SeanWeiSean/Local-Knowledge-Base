# 本地知识库助手 📚

<div align="center">

![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?style=flat-square&logo=dotnet)
![WPF](https://img.shields.io/badge/WPF--UI-4.2.0-0078D4?style=flat-square&logo=windows)
![Ollama](https://img.shields.io/badge/Ollama-Local%20AI-00BCD4?style=flat-square)
![License](https://img.shields.io/badge/License-MIT-green?style=flat-square)

基于 **Ollama** 的本地知识库 WPF 应用程序，支持 Word/Excel 文档的智能问答，采用现代化 Fluent Design 界面。

[功能特点](#功能特点) • [快速开始](#快速开始) • [技术栈](#技术栈) • [项目结构](#项目结构)

</div>

---

## ✨ 功能特点

### 📁 知识库管理
- ✅ **多格式支持** - Word（.docx, .doc）、Excel（.xlsx, .xls）、PowerPoint（.pptx, .ppt）
- ✅ **批量导入** - 支持添加整个文件夹，自动扫描所有文档
- ✅ **智能限制** - 最多 10 个文档，保证查询效率
- ✅ **AI 摘要生成** - 自动提取文档关键内容，生成结构化摘要
- ✅ **一键生成摘要** - 批量为所有未生成摘要的文档生成摘要
- ✅ **摘要缓存** - 删除文档后保留摘要，重新添加无需重复生成
- ✅ **状态可视化** - 清晰显示文档摘要和索引状态

### 🔍 向量语义搜索（RAG）
- ✅ **向量索引** - 使用嵌入模型将文档分块并建立向量索引
- ✅ **语义检索** - 基于余弦相似度的语义搜索，找到最相关的文档片段
- ✅ **两阶段问答** - 先分析相关文档，再基于原文精确回答
- ✅ **向量库管理** - 可视化查看、删除向量索引

### 💬 智能问答
- ✅ **双模式切换** - 知识库模式（文档问答）+ 普通对话模式（纯 AI 聊天）
- ✅ **流式输出** - 实时显示 AI 回复，支持 Token 统计
- ✅ **智能上下文** - 自动筛选相关文档，关键词匹配评分
- ✅ **对话压缩** - 每 3 轮自动压缩历史，保持上下文连贯性
- ✅ **可选中文本** - 聊天内容可复制选中

### 🎨 界面设计
- ✅ **Fluent Design** - 采用 WPF-UI 4.2.0，现代化 Windows 11 风格
- ✅ **Mica 背景** - 半透明云母效果，美观大方
- ✅ **统一字体** - Microsoft YaHei，清晰易读
- ✅ **圆角设计** - 现代化圆角卡片和按钮
- ✅ **醒目开关** - 知识库模式开关，状态一目了然

---

## 🚀 快速开始

### 前置要求

1. **安装 .NET 8.0 SDK**
   ```bash
   winget install Microsoft.DotNet.SDK.8
   ```

2. **安装 Ollama**
   - 下载：https://ollama.ai
   - 安装后拉取必需模型：
     ```bash
     # 聊天模型（必需）
     ollama pull qwen3:1.7b
     
     # 嵌入模型（向量搜索必需）
     ollama pull nomic-embed-text
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

### 运行时依赖

| 依赖 | 版本 | 说明 |
|------|------|------|
| **.NET** | 8.0 | 目标框架 |
| **Ollama** | 最新 | 本地大语言模型服务 |

### Ollama 模型依赖

| 模型 | 用途 | 安装命令 |
|------|------|---------|
| **qwen3:1.7b** | 聊天模型（默认） | `ollama pull qwen3:1.7b` |
| **nomic-embed-text** | 嵌入模型（向量搜索） | `ollama pull nomic-embed-text` |

> 💡 聊天模型可在应用内设置中更换，嵌入模型可在 `App.config` 中配置

### NuGet 包依赖

| 包名 | 版本 | 用途 |
|------|------|------|
| **WPF-UI** | 4.2.0 | Fluent Design UI 框架 |
| **DocumentFormat.OpenXml** | 3.4.1 | Word/PPT 文档处理 |
| **EPPlus** | 8.4.1 | Excel 文档处理 |
| **Newtonsoft.Json** | 13.0.4 | JSON 序列化 |

### 架构

- **WPF** - Windows Presentation Foundation UI 框架
- **MVVM** - Model-View-ViewModel 架构模式
- **RAG** - Retrieval-Augmented Generation 检索增强生成

---

## 📂 项目结构

```
LocalKnowledgeBase/
├── Models/                           # 数据模型
│   ├── DocumentItem.cs              # 文档项模型（支持索引状态）
│   ├── VectorIndexItem.cs           # 向量索引项模型
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
│   ├── ChatService.cs               # Ollama API 调用服务（流式）
│   ├── SummaryCacheService.cs       # 摘要缓存持久化服务
│   ├── EmbeddingService.cs          # 嵌入向量服务
│   └── VectorStoreService.cs        # 向量存储与搜索服务
├── Converters/                       # XAML 值转换器
│   └── BoolToVisibilityConverter.cs # 布尔到可见性转换
├── MainWindow.xaml                   # 主窗口 UI（Fluent Design）
├── MainWindow.xaml.cs               # 主窗口代码隐藏
├── App.xaml                          # 应用程序资源（主题配置）
├── App.xaml.cs                       # 应用程序入口
├── App.config                        # 应用配置（模型设置）
└── LocalKnowledgeBase.csproj         # 项目文件
```

---

## 🎯 核心特性详解

### 两阶段 RAG 问答

**第一阶段：文档分析**
- 通过向量语义搜索找到最相关的文档片段
- AI 分析这些片段与问题的相关性
- 判断是否有足够的信息来回答问题

**第二阶段：精确回答**
- 基于筛选出的相关原文进行回答
- 确保答案有据可依，避免幻觉

### 向量语义搜索

**文档分块**
- 每个文档被分割成 500 字符的块，50 字符重叠
- 使用嵌入模型将每个块转换为向量

**语义检索**
- 用户问题也被转换为向量
- 通过余弦相似度找到最相关的 Top-5 块

**向量存储**
- 向量数据保存在：`%AppData%\LocalKnowledgeBase\vector_store.json`
- 支持增量索引和单独删除

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
%AppData%\LocalKnowledgeBase\summary_cache.json
```

**缓存内容包括：**
- 文档完整路径
- AI 生成的摘要文本
- 摘要文件路径
- 生成时间戳

### 双模式对话

| 模式 | 描述 | 上下文来源 |
|------|------|-----------|
| **知识库模式** | 基于文档内容回答 | 向量检索 + 相关文档摘要 + 压缩历史 |
| **普通对话** | 纯 AI 聊天 | 仅压缩历史 |

---

## ⚙️ 配置说明

### App.config 配置

```xml
<appSettings>
  <add key="OllamaEndpoint" value="http://localhost:11434/api/chat" />
  <add key="ModelName" value="qwen3:1.7b" />
  <add key="EmbeddingModel" value="nomic-embed-text" />
</appSettings>
```

| 配置项 | 说明 | 默认值 |
|--------|------|--------|
| `OllamaEndpoint` | Ollama API 端点 | `http://localhost:11434/api/chat` |
| `ModelName` | 聊天模型名称 | `qwen3:1.7b` |
| `EmbeddingModel` | 嵌入模型名称 | `nomic-embed-text` |

> 💡 聊天模型也可在应用内设置面板中实时修改

### 推荐聊天模型

| 模型 | 大小 | 适用场景 |
|------|------|---------|
| qwen3:1.7b | ~1.2GB | 轻量级，速度快，中文友好 ✅ |
| llama3.2:3b | ~2GB | 平衡性能和速度 |
| qwen3:8b | ~5GB | 高质量回答，需要更多资源 |

### 推荐嵌入模型

| 模型 | 大小 | 说明 |
|------|------|------|
| nomic-embed-text | ~274MB | 高质量文本嵌入，支持 8192 token ✅ |
| mxbai-embed-large | ~670MB | 更大的嵌入维度 |
| all-minilm | ~46MB | 超轻量级，速度最快 |

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
- [WPF-UI](https://github.com/lepoco/wpfui) - Fluent Design UI 组件
- [DocumentFormat.OpenXml](https://github.com/OfficeDev/Open-XML-SDK) - Office 文档处理
- [EPPlus](https://github.com/EPPlusSoftware/EPPlus) - Excel 文件处理
- [nomic-embed-text](https://ollama.ai/library/nomic-embed-text) - 文本嵌入模型

---

<div align="center">

**⭐ 如果这个项目对你有帮助，请给个 Star！⭐**

Made with ❤️ by SeanWeiSean

</div>
