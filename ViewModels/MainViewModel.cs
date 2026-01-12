using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using LocalKnowledgeBase.Models;
using LocalKnowledgeBase.Services;
using Microsoft.Win32;
using System.IO;

namespace LocalKnowledgeBase.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        private readonly IDocumentService _documentService;
        private readonly IChatService _chatService;
        private readonly SummaryCacheService _summaryCacheService;

        // 请求滚动到底部的事件
        public event Action? ScrollToBottomRequested;

        public MainViewModel()
        {
            _documentService = new DocumentService();
            _chatService = new ChatService();
            _summaryCacheService = new SummaryCacheService();            Documents = new ObservableCollection<DocumentItem>();
            ChatMessages = new ObservableCollection<ChatMessage>();            // 初始化命令
            AddFileCommand = new RelayCommand(OnAddFile);
            AddFolderCommand = new RelayCommand(OnAddFolder);
            RemoveDocumentCommand = new RelayCommand<DocumentItem>(OnRemoveDocument);
            GenerateSummaryCommand = new RelayCommand<DocumentItem>(OnGenerateSummary, CanGenerateSummary);
            RemoveSummaryCommand = new RelayCommand<DocumentItem>(OnRemoveSummary, CanRemoveSummary);
            SendMessageCommand = new RelayCommand(OnSendMessage, CanSendMessage);
            ToggleSettingsCommand = new RelayCommand(OnToggleSettings);

            // 初始化模型设置
            _modelName = _chatService.ModelName;
            _ollamaEndpoint = _chatService.Endpoint;

            // 添加欢迎消息
            ChatMessages.Add(new ChatMessage
            {
                Role = MessageRole.Assistant,
                Content = "您好！我是本地知识库助手。请先在左侧添加文档并生成摘要，然后您就可以向我提问了。\n\n💡 提示：点击右上角的⚙️按钮可以配置AI模型。"
            });
        }

        private void OnToggleSettings(object? parameter)
        {
            IsSettingsOpen = !IsSettingsOpen;
        }

        #region Properties

        private ObservableCollection<DocumentItem> _documents = null!;
        public ObservableCollection<DocumentItem> Documents
        {
            get => _documents;
            set => SetProperty(ref _documents, value);
        }

        private ObservableCollection<ChatMessage> _chatMessages = null!;
        public ObservableCollection<ChatMessage> ChatMessages
        {
            get => _chatMessages;
            set => SetProperty(ref _chatMessages, value);
        }

        private string _userInput = string.Empty;
        public string UserInput
        {
            get => _userInput;
            set => SetProperty(ref _userInput, value);
        }

        private bool _isProcessing = false;
        public bool IsProcessing
        {
            get => _isProcessing;
            set => SetProperty(ref _isProcessing, value);
        }        private bool _useKnowledgeBase = true;
        public bool UseKnowledgeBase
        {
            get => _useKnowledgeBase;
            set => SetProperty(ref _useKnowledgeBase, value);
        }

        // 设置面板相关属性
        private bool _isSettingsOpen = false;
        public bool IsSettingsOpen
        {
            get => _isSettingsOpen;
            set => SetProperty(ref _isSettingsOpen, value);
        }

        private string _modelName = "qwen2.5:1.5b";
        public string ModelName
        {
            get => _modelName;
            set
            {
                if (SetProperty(ref _modelName, value))
                {
                    _chatService.ModelName = value;
                }
            }
        }

        private string _ollamaEndpoint = "http://localhost:11434/api/chat";
        public string OllamaEndpoint
        {
            get => _ollamaEndpoint;
            set
            {
                if (SetProperty(ref _ollamaEndpoint, value))
                {
                    _chatService.Endpoint = value;
                }
            }
        }

        // 对话历史（用于上下文）
        private List<ChatMessage> _conversationHistory = new List<ChatMessage>();
        private string _compressedHistory = string.Empty;
        private int _messagesSinceLastCompression = 0;

        #endregion

        #region Commands

        public ICommand AddFileCommand { get; }
        public ICommand AddFolderCommand { get; }
        public ICommand RemoveDocumentCommand { get; }
        public ICommand GenerateSummaryCommand { get; }
        public ICommand SendMessageCommand { get; }
        public ICommand ToggleSettingsCommand { get; }
        public ICommand RemoveSummaryCommand { get; private set; }

        #endregion

        #region Command Handlers

        private void OnAddFile(object? parameter)
        {
            if (Documents.Count >= 10)
            {
                MessageBox.Show("最多只能添加10个文档，请删除后再添加。", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var openFileDialog = new OpenFileDialog
            {
                Filter = "Office Documents|*.docx;*.doc;*.xlsx;*.xls|Word Documents|*.docx;*.doc|Excel Documents|*.xlsx;*.xls|All files|*.*",
                Multiselect = true
            };

            if (openFileDialog.ShowDialog() == true)
            {
                foreach (var fileName in openFileDialog.FileNames)
                {
                    if (Documents.Count >= 10)
                    {
                        MessageBox.Show($"已达到最大文档数量限制（10个）。", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                        break;
                    }

                    var extension = Path.GetExtension(fileName).ToLower();
                    var docType = extension.Contains("doc") ? DocumentType.Word : DocumentType.Excel;

                    var document = new DocumentItem
                    {
                        FilePath = fileName,
                        FileName = Path.GetFileName(fileName),
                        Type = docType
                    };

                    // 检查是否已有缓存的摘要
                    if (_summaryCacheService.HasCachedSummary(fileName))
                    {
                        var cached = _summaryCacheService.GetCachedSummary(fileName);
                        if (cached != null)
                        {
                            document.Summary = cached.Summary;
                            document.SummaryPath = cached.SummaryPath;
                            document.IsSummarized = true;
                        }
                    }

                    Documents.Add(document);
                }
            }
        }

        private void OnAddFolder(object? parameter)
        {
            if (Documents.Count >= 10)
            {
                MessageBox.Show("最多只能添加10个文档，请删除后再添加。", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var dialog = new System.Windows.Forms.FolderBrowserDialog
            {
                Description = "选择要添加到知识库的文件夹"
            };

            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                var folder = dialog.SelectedPath;

                // 自动添加文件夹内的所有Word和Excel文件
                var files = Directory.GetFiles(folder, "*.*", SearchOption.AllDirectories)
                    .Where(f => f.EndsWith(".docx", StringComparison.OrdinalIgnoreCase) ||
                               f.EndsWith(".doc", StringComparison.OrdinalIgnoreCase) ||
                               f.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase) ||
                               f.EndsWith(".xls", StringComparison.OrdinalIgnoreCase));

                foreach (var file in files)
                {
                    if (Documents.Count >= 10)
                    {
                        MessageBox.Show($"已达到最大文档数量限制（10个）。", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                        break;
                    }

                    var extension = Path.GetExtension(file).ToLower();
                    var docType = extension.Contains("doc") ? DocumentType.Word : DocumentType.Excel;

                    var fileDoc = new DocumentItem
                    {
                        FilePath = file,
                        FileName = Path.GetFileName(file),
                        Type = docType
                    };

                    // 检查是否已有缓存的摘要
                    if (_summaryCacheService.HasCachedSummary(file))
                    {
                        var cached = _summaryCacheService.GetCachedSummary(file);
                        if (cached != null)
                        {
                            fileDoc.Summary = cached.Summary;
                            fileDoc.SummaryPath = cached.SummaryPath;
                            fileDoc.IsSummarized = true;
                        }
                    }

                    Documents.Add(fileDoc);
                }
            }
        }

        private void OnRemoveDocument(DocumentItem? document)
        {
            if (document != null)
            {
                Documents.Remove(document);
            }
        }

        private bool CanGenerateSummary(DocumentItem? document)
        {
            return document != null && !document.IsSummarized && !IsProcessing;
        }

        private async void OnGenerateSummary(DocumentItem? document)
        {
            if (document == null) return;

            IsProcessing = true;
            try
            {
                // 提取文档文本
                string text = await _documentService.ExtractTextAsync(document.FilePath, document.Type);

                // 调用AI生成摘要
                string summary = await _chatService.GenerateSummaryAsync(text);

                // 保存摘要
                string summaryPath = await _documentService.SaveSummaryAsync(document.FilePath, summary);

                document.Summary = summary;
                document.SummaryPath = summaryPath;
                document.IsSummarized = true;

                // 保存到缓存
                _summaryCacheService.SaveSummary(document.FilePath, summary, summaryPath);

                OnPropertyChanged(nameof(Documents));
            }
            catch (Exception ex)
            {
                MessageBox.Show($"生成摘要失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsProcessing = false;
            }
        }

        private bool CanSendMessage(object? parameter)
        {
            return !string.IsNullOrWhiteSpace(UserInput) && !IsProcessing;
        }        private async void OnSendMessage(object? parameter)
        {
            if (string.IsNullOrWhiteSpace(UserInput)) return;

            var userMessage = new ChatMessage
            {
                Role = MessageRole.User,
                Content = UserInput
            };

            ChatMessages.Add(userMessage);
            _conversationHistory.Add(userMessage);
            ScrollToBottomRequested?.Invoke();

            var userQuestion = UserInput;
            UserInput = string.Empty;

            // 创建 AI 回复消息（流式填充）
            var assistantMessage = new ChatMessage
            {
                Role = MessageRole.Assistant,
                Content = "",
                IsLoading = true
            };
            ChatMessages.Add(assistantMessage);
            ScrollToBottomRequested?.Invoke();

            IsProcessing = true;
            var startTime = DateTime.Now;
            
            try
            {
                string context = string.Empty;
                
                if (UseKnowledgeBase)
                {
                    // 获取知识库上下文
                    context = await GetKnowledgeBaseContext(userQuestion);
                }
                else if (!string.IsNullOrWhiteSpace(_compressedHistory))
                {
                    context = _compressedHistory;
                }

                // 使用流式请求
                await _chatService.AskQuestionStreamAsync(userQuestion, context, (chunk, finalResponse) =>
                {
                    Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        assistantMessage.Content += chunk;
                        ScrollToBottomRequested?.Invoke();

                        // 当收到最终响应时，计算 token 统计
                        if (finalResponse != null)
                        {
                            var elapsed = (DateTime.Now - startTime).TotalSeconds;
                            var evalCount = finalResponse.eval_count;
                            var tokensPerSecond = elapsed > 0 ? evalCount / elapsed : 0;
                            
                            assistantMessage.TokenStats = $"📊 {evalCount} tokens · {tokensPerSecond:F1} tokens/s · {elapsed:F1}s";
                            assistantMessage.IsLoading = false;
                        }
                    }));
                });

                _conversationHistory.Add(assistantMessage);
                
                // 定期压缩对话历史（每3轮）
                _messagesSinceLastCompression++;
                if (_messagesSinceLastCompression >= 3)
                {
                    await CompressConversationHistory();
                }
            }
            catch (Exception ex)
            {
                assistantMessage.Content = $"抱歉，发生了错误: {ex.Message}";
                assistantMessage.IsLoading = false;
            }
            finally
            {
                IsProcessing = false;
            }
        }

        private bool CanRemoveSummary(DocumentItem? document)
        {
            return document != null && document.IsSummarized && !IsProcessing;
        }

        private void OnRemoveSummary(DocumentItem? document)
        {
            if (document == null) return;

            try
            {
                // 删除摘要文件
                if (!string.IsNullOrEmpty(document.SummaryPath) && File.Exists(document.SummaryPath))
                {
                    File.Delete(document.SummaryPath);
                }

                // 从缓存中移除
                _summaryCacheService.RemoveSummary(document.FilePath);

                // 更新文档状态
                document.Summary = string.Empty;
                document.SummaryPath = string.Empty;
                document.IsSummarized = false;

                // 通知UI更新
                OnPropertyChanged(nameof(Documents));
            }
            catch (Exception ex)
            {
                MessageBox.Show($"删除摘要失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 根据问题筛选相关文档
        /// </summary>
        private List<DocumentItem> FindRelevantDocuments(string question)
        {
            var allDocs = Documents.Where(d => d.IsSummarized).ToList();
            if (allDocs.Count == 0) return new List<DocumentItem>();

            // 简单关键词匹配：检查问题中的关键词是否在文档名或摘要中
            var questionLower = question.ToLower();
            var scored = allDocs.Select(doc => new
            {
                Document = doc,
                Score = CalculateRelevanceScore(questionLower, doc)
            }).Where(x => x.Score > 0)
              .OrderByDescending(x => x.Score)
              .Take(3) // 最多取3个相关文档
              .Select(x => x.Document)
              .ToList();

            // 如果没有匹配到，返回所有文档（但最多3个）
            return scored.Count > 0 ? scored : allDocs.Take(3).ToList();
        }

        /// <summary>
        /// 计算文档相关性分数
        /// </summary>
        private int CalculateRelevanceScore(string questionLower, DocumentItem doc)
        {
            int score = 0;
            var fileNameLower = doc.FileName.ToLower();
            var summaryLower = (doc.Summary ?? "").ToLower();

            // 提取关键词（简单分词）
            var keywords = questionLower.Split(new[] { ' ', '，', '。', '？', '！', '\n', '\r' }, 
                StringSplitOptions.RemoveEmptyEntries)
                .Where(w => w.Length > 1)
                .ToList();            foreach (var keyword in keywords)
            {
                if (fileNameLower.Contains(keyword)) score += 10;
                if (summaryLower.Contains(keyword)) score += 5;
            }

            return score;
        }

        /// <summary>
        /// 压缩对话历史
        /// </summary>
        private async Task CompressConversationHistory()
        {
            if (_conversationHistory.Count < 4) return; // 太少不压缩

            try
            {
                // 构建对话历史文本
                var historyText = string.Join("\n", _conversationHistory.Select(m => 
                    $"{(m.Role == MessageRole.User ? "用户" : "AI")}: {m.Content}"));

                // 调用AI压缩
                _compressedHistory = await _chatService.CompressConversationAsync(historyText);
                
                // 清空历史，重置计数
                _conversationHistory.Clear();
                _messagesSinceLastCompression = 0;
            }
            catch
            {
                // 压缩失败，保持原样
            }
        }        /// <summary>
        /// 处理知识库问题：同时使用摘要和原文
        /// </summary>
        private async Task<string> ProcessKnowledgeBaseQuestion(string userQuestion)
        {
            var context = await GetKnowledgeBaseContext(userQuestion);
            if (string.IsNullOrEmpty(context))
            {
                return "抱歉，当前没有可用的知识库文档。请先添加文档并生成摘要，或关闭知识库模式进行普通对话。";
            }
            return await _chatService.AskQuestionAsync(userQuestion, context);
        }

        /// <summary>
        /// 获取知识库上下文
        /// </summary>
        private async Task<string> GetKnowledgeBaseContext(string userQuestion)
        {
            // 获取所有已生成摘要的文档
            var relevantDocs = Documents.Where(d => d.IsSummarized).Take(3).ToList();
            
            if (relevantDocs.Count == 0)
            {
                return string.Empty;
            }

            try
            {
                // 构建包含摘要和原文的完整上下文
                var contextParts = new List<string>();
                
                foreach (var doc in relevantDocs)
                {
                    try
                    {
                        // 读取原文
                        string fullText = await _documentService.ExtractTextAsync(doc.FilePath, doc.Type);
                        
                        // 同时包含摘要和原文
                        var docContext = $"[文档: {doc.FileName}]\n" +
                                        $"【摘要】\n{doc.Summary}\n\n" +
                                        $"【原文】\n{fullText}";
                        contextParts.Add(docContext);
                    }
                    catch
                    {
                        // 如果读取原文失败，只使用摘要
                        contextParts.Add($"[文档: {doc.FileName}]\n【摘要】\n{doc.Summary}");
                    }
                }

                var fullContext = string.Join("\n\n---\n\n", contextParts);
                
                if (!string.IsNullOrWhiteSpace(_compressedHistory))
                {
                    fullContext = $"对话历史摘要：\n{_compressedHistory}\n\n{fullContext}";
                }

                return fullContext;
            }
            catch (Exception)
            {
                return string.Empty;
            }
        }

        #endregion
    }
}
