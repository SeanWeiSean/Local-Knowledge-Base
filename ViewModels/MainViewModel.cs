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
        private readonly VectorStoreService _vectorStoreService;

        // 请求滚动到底部的事件
        public event Action? ScrollToBottomRequested;

        public MainViewModel()
        {
            _documentService = new DocumentService();
            _chatService = new ChatService();
            _summaryCacheService = new SummaryCacheService();
            _vectorStoreService = new VectorStoreService();

            Documents = new ObservableCollection<DocumentItem>();
            ChatMessages = new ObservableCollection<ChatMessage>();            // 初始化命令
            AddFileCommand = new RelayCommand(OnAddFile);
            AddFolderCommand = new RelayCommand(OnAddFolder);
            RemoveDocumentCommand = new RelayCommand<DocumentItem>(OnRemoveDocument);
            GenerateSummaryCommand = new RelayCommand<DocumentItem>(OnGenerateSummary, CanGenerateSummary);
            GenerateAllSummariesCommand = new RelayCommand(OnGenerateAllSummaries, CanGenerateAllSummaries);
            RemoveSummaryCommand = new RelayCommand<DocumentItem>(OnRemoveSummary, CanRemoveSummary);
            SendMessageCommand = new RelayCommand(OnSendMessage, CanSendMessage);
            ToggleSettingsCommand = new RelayCommand(OnToggleSettings);
            ToggleVectorStoreCommand = new RelayCommand(OnToggleVectorStore);
            DeleteVectorIndexCommand = new RelayCommand<VectorIndexItem>(OnDeleteVectorIndex);
            ClearAllVectorIndexCommand = new RelayCommand(OnClearAllVectorIndex);

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

        private void OnToggleVectorStore(object? parameter)
        {
            IsVectorStoreOpen = !IsVectorStoreOpen;
            if (IsVectorStoreOpen)
            {
                RefreshVectorStoreItems();
            }
        }

        private async void OnDeleteVectorIndex(VectorIndexItem? item)
        {
            if (item == null) return;
            
            await _vectorStoreService.RemoveDocumentAsync(item.FilePath);
            RefreshVectorStoreItems();
            
            // 更新文档列表中对应文档的索引状态
            var doc = Documents.FirstOrDefault(d => d.FilePath == item.FilePath);
            if (doc != null)
            {
                doc.IsIndexed = false;
                doc.ChunkCount = 0;
            }
        }

        private async void OnClearAllVectorIndex(object? parameter)
        {
            await _vectorStoreService.ClearAllAsync();
            RefreshVectorStoreItems();
            
            // 更新所有文档的索引状态
            foreach (var doc in Documents)
            {
                doc.IsIndexed = false;
                doc.ChunkCount = 0;
            }
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

        // 向量库管理面板
        private bool _isVectorStoreOpen = false;
        public bool IsVectorStoreOpen
        {
            get => _isVectorStoreOpen;
            set
            {
                if (SetProperty(ref _isVectorStoreOpen, value) && value)
                {
                    RefreshVectorStoreItems();
                }
            }
        }

        private ObservableCollection<VectorIndexItem> _vectorStoreItems = new();
        public ObservableCollection<VectorIndexItem> VectorStoreItems
        {
            get => _vectorStoreItems;
            set => SetProperty(ref _vectorStoreItems, value);
        }

        private string _vectorStoreStats = string.Empty;
        public string VectorStoreStats
        {
            get => _vectorStoreStats;
            set => SetProperty(ref _vectorStoreStats, value);
        }

        private void RefreshVectorStoreItems()
        {
            VectorStoreItems.Clear();
            var items = _vectorStoreService.GetIndexSummary();
            foreach (var item in items)
            {
                VectorStoreItems.Add(new VectorIndexItem
                {
                    FilePath = item.FilePath,
                    FileName = item.FileName,
                    ChunkCount = item.ChunkCount
                });
            }
            VectorStoreStats = $"共 {items.Count} 个文档，{_vectorStoreService.GetTotalChunkCount()} 个向量块";
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
        public ICommand GenerateAllSummariesCommand { get; }
        public ICommand SendMessageCommand { get; }
        public ICommand ToggleSettingsCommand { get; }
        public ICommand RemoveSummaryCommand { get; private set; }
        public ICommand ToggleVectorStoreCommand { get; }
        public ICommand DeleteVectorIndexCommand { get; }
        public ICommand ClearAllVectorIndexCommand { get; }

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
                Filter = "Office Documents|*.docx;*.doc;*.xlsx;*.xls;*.pptx;*.ppt|Word Documents|*.docx;*.doc|Excel Documents|*.xlsx;*.xls|PowerPoint Documents|*.pptx;*.ppt|All files|*.*",
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
                    var docType = extension.Contains("doc") ? DocumentType.Word : 
                                  extension.Contains("xls") ? DocumentType.Excel : 
                                  extension.Contains("ppt") ? DocumentType.PowerPoint : DocumentType.Word;

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
                               f.EndsWith(".xls", StringComparison.OrdinalIgnoreCase) ||
                               f.EndsWith(".pptx", StringComparison.OrdinalIgnoreCase) ||
                               f.EndsWith(".ppt", StringComparison.OrdinalIgnoreCase));

                foreach (var file in files)
                {
                    if (Documents.Count >= 10)
                    {
                        MessageBox.Show($"已达到最大文档数量限制（10个）。", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                        break;
                    }

                    var extension = Path.GetExtension(file).ToLower();
                    var docType = extension.Contains("doc") ? DocumentType.Word : 
                                  extension.Contains("xls") ? DocumentType.Excel : 
                                  extension.Contains("ppt") ? DocumentType.PowerPoint : DocumentType.Word;

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

                // 建立向量索引
                try
                {
                    await _vectorStoreService.IndexDocumentAsync(document.FilePath, document.FileName, text);
                    document.IsIndexed = true;
                    document.ChunkCount = _vectorStoreService.GetDocumentChunkCount(document.FilePath);
                }
                catch
                {
                    // 向量索引失败不影响主流程
                }

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

        private bool CanGenerateAllSummaries(object? parameter)
        {
            return Documents.Any(d => !d.IsSummarized) && !IsProcessing;
        }

        private async void OnGenerateAllSummaries(object? parameter)
        {
            var docsToProcess = Documents.Where(d => !d.IsSummarized).ToList();
            if (docsToProcess.Count == 0)
            {
                MessageBox.Show("所有文档都已生成摘要！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            IsProcessing = true;
            int successCount = 0;
            int failCount = 0;

            // 添加进度提示消息
            var progressMessage = new ChatMessage
            {
                Role = MessageRole.Assistant,
                Content = $"📝 开始批量生成摘要，共 {docsToProcess.Count} 个文档...\n",
                IsLoading = true
            };
            ChatMessages.Add(progressMessage);
            ScrollToBottomRequested?.Invoke();

            try
            {
                for (int i = 0; i < docsToProcess.Count; i++)
                {
                    var doc = docsToProcess[i];
                    var currentIndex = i + 1;  // 创建局部副本避免闭包问题
                    var totalCount = docsToProcess.Count;
                    
                    try
                    {
                        // 更新进度
                        Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                        {
                            progressMessage.Content = $"📝 正在生成摘要 ({currentIndex}/{totalCount})...\n\n当前文档：{doc.FileName}";
                        }));

                        // 提取文档文本
                        string text = await _documentService.ExtractTextAsync(doc.FilePath, doc.Type);

                        // 调用AI生成摘要
                        string summary = await _chatService.GenerateSummaryAsync(text);

                        // 保存摘要
                        string summaryPath = await _documentService.SaveSummaryAsync(doc.FilePath, summary);

                        doc.Summary = summary;
                        doc.SummaryPath = summaryPath;
                        doc.IsSummarized = true;

                        // 保存到缓存
                        _summaryCacheService.SaveSummary(doc.FilePath, summary, summaryPath);

                        // 建立向量索引
                        Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                        {
                            progressMessage.Content = $"🔢 正在建立索引 ({currentIndex}/{totalCount})...\n\n当前文档：{doc.FileName}";
                        }));

                        try
                        {
                            await _vectorStoreService.IndexDocumentAsync(doc.FilePath, doc.FileName, text);
                            doc.IsIndexed = true;
                            doc.ChunkCount = _vectorStoreService.GetDocumentChunkCount(doc.FilePath);
                        }
                        catch
                        {
                            // 向量索引失败不影响主流程
                        }

                        successCount++;
                    }
                    catch (Exception ex)
                    {
                        failCount++;
                        Application.Current.Dispatcher.Invoke(new Action(() =>
                        {
                            progressMessage.Content += $"\n❌ {doc.FileName}: {ex.Message}";
                        }));
                    }
                }

                OnPropertyChanged(nameof(Documents));
                
                // 更新最终结果（在 UI 线程上）
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    progressMessage.Content = $"✅ 批量生成完成！\n\n" +
                                              $"成功：{successCount} 个（含向量索引）\n" +
                                              (failCount > 0 ? $"失败：{failCount} 个" : "");
                    progressMessage.IsLoading = false;
                });
                ScrollToBottomRequested?.Invoke();
            }
            catch (Exception ex)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    progressMessage.Content += $"\n\n❌ 批量处理出错: {ex.Message}";
                    progressMessage.IsLoading = false;
                });
                progressMessage.IsLoading = false;
            }
            finally
            {
                IsProcessing = false;
            }
        }

        private bool CanSendMessage(object? parameter)
        {
            return !string.IsNullOrWhiteSpace(UserInput) && !IsProcessing;
        }

        private async void OnSendMessage(object? parameter)
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

            IsProcessing = true;
            
            try
            {
                if (UseKnowledgeBase && Documents.Any(d => d.IsSummarized))
                {
                    // === 两阶段问答模式 ===
                    await TwoStageKnowledgeBaseQuery(userQuestion);
                }
                else
                {
                    // === 普通对话模式 ===
                    await SingleStageQuery(userQuestion);
                }
                
                // 定期压缩对话历史（每3轮）
                _messagesSinceLastCompression++;
                if (_messagesSinceLastCompression >= 3)
                {
                    await CompressConversationHistory();
                }
            }
            catch (Exception ex)
            {
                var errorMessage = new ChatMessage
                {
                    Role = MessageRole.Assistant,
                    Content = $"抱歉，发生了错误: {ex.Message}"
                };
                ChatMessages.Add(errorMessage);
            }
            finally
            {
                IsProcessing = false;
            }
        }

        /// <summary>
        /// 两阶段知识库问答（使用向量检索）
        /// </summary>
        private async Task TwoStageKnowledgeBaseQuery(string userQuestion)
        {
            var summarizedDocs = Documents.Where(d => d.IsSummarized).ToList();
            
            // === 第一阶段：向量检索相关内容 ===
            var stage1Message = new ChatMessage
            {
                Role = MessageRole.Assistant,
                Content = "🔍 **第一阶段：向量检索相关内容...**\n\n",
                IsLoading = true
            };
            ChatMessages.Add(stage1Message);
            ScrollToBottomRequested?.Invoke();

            var startTime1 = DateTime.Now;
            List<SearchResult> searchResults;

            try
            {
                // 使用向量检索
                searchResults = await _vectorStoreService.SearchAsync(userQuestion, topK: 5);
                
                if (searchResults.Count == 0)
                {
                    // 向量库为空，回退到 AI 判断模式
                    stage1Message.Content += "⚠️ 向量索引为空，请先为文档建立索引。\n\n";
                    stage1Message.Content += "💡 提示：生成摘要时会自动建立向量索引，或点击文档的「建立索引」按钮。";
                    stage1Message.IsLoading = false;
                    
                    var elapsed1 = (DateTime.Now - startTime1).TotalSeconds;
                    stage1Message.TokenStats = $"⏱️ {elapsed1:F1}s";
                    _conversationHistory.Add(stage1Message);
                    return;
                }

                // 显示检索结果
                stage1Message.Content += $"✅ 找到 {searchResults.Count} 个相关片段：\n\n";
                
                var docGroups = searchResults.GroupBy(r => r.Chunk.FileName);
                foreach (var group in docGroups)
                {
                    var avgScore = group.Average(r => r.Score);
                    stage1Message.Content += $"📎 **{group.Key}** (相似度: {avgScore:P0})\n";
                    foreach (var result in group.Take(2))
                    {
                        var preview = result.Chunk.Content.Length > 100 
                            ? result.Chunk.Content.Substring(0, 100) + "..." 
                            : result.Chunk.Content;
                        stage1Message.Content += $"   └ 「{preview}」\n";
                    }
                    stage1Message.Content += "\n";
                }

                var elapsed = (DateTime.Now - startTime1).TotalSeconds;
                stage1Message.TokenStats = $"⚡ 向量检索 · {elapsed:F2}s";
                stage1Message.IsLoading = false;
            }
            catch (Exception ex)
            {
                stage1Message.Content += $"❌ 向量检索失败: {ex.Message}\n\n回退到摘要匹配模式...";
                stage1Message.IsLoading = false;
                _conversationHistory.Add(stage1Message);
                
                // 回退到旧模式
                await FallbackToSummaryMatching(userQuestion, summarizedDocs);
                return;
            }

            _conversationHistory.Add(stage1Message);

            // === 第二阶段：基于检索内容回答 ===
            var stage2Message = new ChatMessage
            {
                Role = MessageRole.Assistant,
                Content = "📖 **第二阶段：基于检索内容回答...**\n\n",
                IsLoading = true
            };
            ChatMessages.Add(stage2Message);
            ScrollToBottomRequested?.Invoke();

            var startTime2 = DateTime.Now;

            // 使用检索到的内容块作为上下文
            var contextParts = searchResults
                .Select(r => $"【{r.Chunk.FileName} - 片段{r.Chunk.ChunkIndex + 1}】\n{r.Chunk.Content}")
                .ToList();
            
            var fullContext = string.Join("\n\n---\n\n", contextParts);

            await _chatService.AskQuestionStreamAsync(userQuestion, fullContext, (chunk, finalResponse) =>
            {
                Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    stage2Message.Content += chunk;
                    ScrollToBottomRequested?.Invoke();

                    if (finalResponse != null)
                    {
                        var elapsed = (DateTime.Now - startTime2).TotalSeconds;
                        var evalCount = finalResponse.eval_count;
                        var tokensPerSecond = elapsed > 0 ? evalCount / elapsed : 0;
                        stage2Message.TokenStats = $"📊 {evalCount} tokens · {tokensPerSecond:F1} tokens/s · {elapsed:F1}s";
                        stage2Message.IsLoading = false;
                    }
                }));
            });

            _conversationHistory.Add(stage2Message);
        }

        /// <summary>
        /// 回退到摘要匹配模式（当向量检索不可用时）
        /// </summary>
        private async Task FallbackToSummaryMatching(string userQuestion, List<DocumentItem> summarizedDocs)
        {
            var stage1Message = new ChatMessage
            {
                Role = MessageRole.Assistant,
                Content = "🔍 **使用 AI 分析相关文档...**\n\n",
                IsLoading = true
            };
            ChatMessages.Add(stage1Message);
            ScrollToBottomRequested?.Invoke();

            var startTime1 = DateTime.Now;
            var relatedDocNames = new List<string>();

            var docSummaries = string.Join("\n\n", summarizedDocs.Select((doc, i) => 
                $"【文档 {i + 1}】{doc.FileName}\n摘要：{doc.Summary}"));

            await _chatService.FindRelevantDocumentsStreamAsync(userQuestion, docSummaries, (chunk, finalResponse) =>
            {
                Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    stage1Message.Content += chunk;
                    ScrollToBottomRequested?.Invoke();

                    if (finalResponse != null)
                    {
                        var elapsed = (DateTime.Now - startTime1).TotalSeconds;
                        var evalCount = finalResponse.eval_count;
                        var tokensPerSecond = elapsed > 0 ? evalCount / elapsed : 0;
                        stage1Message.TokenStats = $"📊 {evalCount} tokens · {tokensPerSecond:F1} tokens/s · {elapsed:F1}s";
                        stage1Message.IsLoading = false;
                    }
                }));
            });

            foreach (var doc in summarizedDocs)
            {
                if (stage1Message.Content.Contains(doc.FileName))
                {
                    relatedDocNames.Add(doc.FileName);
                }
            }

            _conversationHistory.Add(stage1Message);

            var relevantDocs = relatedDocNames.Count > 0 
                ? summarizedDocs.Where(d => relatedDocNames.Contains(d.FileName)).ToList()
                : summarizedDocs.Take(2).ToList();

            // 第二阶段
            var stage2Message = new ChatMessage
            {
                Role = MessageRole.Assistant,
                Content = "📖 **第二阶段：基于原文详细回答...**\n\n",
                IsLoading = true
            };
            ChatMessages.Add(stage2Message);
            ScrollToBottomRequested?.Invoke();

            var startTime2 = DateTime.Now;

            var contextParts = new List<string>();
            foreach (var doc in relevantDocs)
            {
                try
                {
                    string fullText = await _documentService.ExtractTextAsync(doc.FilePath, doc.Type);
                    if (fullText.Length > 4000)
                    {
                        fullText = fullText.Substring(0, 4000) + "...[内容已截断]";
                    }
                    contextParts.Add($"【{doc.FileName}】\n{fullText}");
                }
                catch
                {
                    contextParts.Add($"【{doc.FileName}】\n{doc.Summary}");
                }
            }

            var fullContext = string.Join("\n\n---\n\n", contextParts);

            await _chatService.AskQuestionStreamAsync(userQuestion, fullContext, (chunk, finalResponse) =>
            {
                Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    stage2Message.Content += chunk;
                    ScrollToBottomRequested?.Invoke();

                    if (finalResponse != null)
                    {
                        var elapsed = (DateTime.Now - startTime2).TotalSeconds;
                        var evalCount = finalResponse.eval_count;
                        var tokensPerSecond = elapsed > 0 ? evalCount / elapsed : 0;
                        stage2Message.TokenStats = $"📊 {evalCount} tokens · {tokensPerSecond:F1} tokens/s · {elapsed:F1}s";
                        stage2Message.IsLoading = false;
                    }
                }));
            });

            _conversationHistory.Add(stage2Message);
        }

        /// <summary>
        /// 单阶段普通问答
        /// </summary>
        private async Task SingleStageQuery(string userQuestion)
        {
            var assistantMessage = new ChatMessage
            {
                Role = MessageRole.Assistant,
                Content = "",
                IsLoading = true
            };
            ChatMessages.Add(assistantMessage);
            ScrollToBottomRequested?.Invoke();

            var startTime = DateTime.Now;

            string context = string.Empty;
            if (!string.IsNullOrWhiteSpace(_compressedHistory))
            {
                context = _compressedHistory;
            }

            await _chatService.AskQuestionStreamAsync(userQuestion, context, (chunk, finalResponse) =>
            {
                Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    assistantMessage.Content += chunk;
                    ScrollToBottomRequested?.Invoke();

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
