using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace LocalKnowledgeBase.Services
{
    /// <summary>
    /// 本地向量存储服务 - 支持文档分块和向量检索
    /// </summary>
    public class VectorStoreService
    {
        private readonly string _storePath;
        private readonly EmbeddingService _embeddingService;
        private VectorDatabase _database;

        public VectorStoreService()
        {
            _embeddingService = new EmbeddingService();
            _storePath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "LocalKnowledgeBase",
                "vector_store.json");
            
            _database = LoadDatabase();
        }

        /// <summary>
        /// 将文档分块并存储向量
        /// </summary>
        public async Task IndexDocumentAsync(string filePath, string fileName, string content, IProgress<string>? progress = null)
        {
            // 先删除该文档的旧数据
            RemoveDocument(filePath);

            // 分块
            var chunks = ChunkText(content, chunkSize: 500, overlap: 50);
            progress?.Report($"📦 文档分成 {chunks.Count} 个块");

            // 为每个块生成向量
            for (int i = 0; i < chunks.Count; i++)
            {
                progress?.Report($"🔢 生成向量 ({i + 1}/{chunks.Count})...");
                
                try
                {
                    var embedding = await _embeddingService.GetEmbeddingAsync(chunks[i]);
                    
                    var chunk = new DocumentChunk
                    {
                        Id = Guid.NewGuid().ToString(),
                        FilePath = filePath,
                        FileName = fileName,
                        ChunkIndex = i,
                        Content = chunks[i],
                        Embedding = embedding
                    };
                    
                    _database.Chunks.Add(chunk);
                }
                catch (Exception ex)
                {
                    progress?.Report($"⚠️ 块 {i + 1} 向量化失败: {ex.Message}");
                }
            }

            SaveDatabase();
            progress?.Report($"✅ 已索引 {chunks.Count} 个文档块");
        }

        /// <summary>
        /// 搜索相关文档块
        /// </summary>
        public async Task<List<SearchResult>> SearchAsync(string query, int topK = 5)
        {
            if (_database.Chunks.Count == 0)
            {
                return new List<SearchResult>();
            }

            // 获取查询向量
            var queryEmbedding = await _embeddingService.GetEmbeddingAsync(query);

            // 计算相似度并排序
            var results = _database.Chunks
                .Select(chunk => new SearchResult
                {
                    Chunk = chunk,
                    Score = CosineSimilarity(queryEmbedding, chunk.Embedding)
                })
                .OrderByDescending(r => r.Score)
                .Take(topK)
                .ToList();

            return results;
        }

        /// <summary>
        /// 移除文档的所有向量
        /// </summary>
        public void RemoveDocument(string filePath)
        {
            _database.Chunks.RemoveAll(c => c.FilePath == filePath);
            SaveDatabase();
        }

        /// <summary>
        /// 检查文档是否已索引
        /// </summary>
        public bool IsDocumentIndexed(string filePath)
        {
            return _database.Chunks.Any(c => c.FilePath == filePath);
        }

        /// <summary>
        /// 获取文档的分块数量
        /// </summary>
        public int GetDocumentChunkCount(string filePath)
        {
            return _database.Chunks.Count(c => c.FilePath == filePath);
        }

        /// <summary>
        /// 获取已索引的文档列表
        /// </summary>
        public List<string> GetIndexedDocuments()
        {
            return _database.Chunks
                .Select(c => c.FilePath)
                .Distinct()
                .ToList();
        }

        /// <summary>
        /// 获取索引统计信息（用于 UI 显示）
        /// </summary>
        public List<(string FilePath, string FileName, int ChunkCount)> GetIndexSummary()
        {
            return _database.Chunks
                .GroupBy(c => new { c.FilePath, c.FileName })
                .Select(g => (g.Key.FilePath, g.Key.FileName, g.Count()))
                .ToList();
        }

        /// <summary>
        /// 获取总块数
        /// </summary>
        public int GetTotalChunkCount()
        {
            return _database.Chunks.Count;
        }

        /// <summary>
        /// 异步移除文档的所有向量
        /// </summary>
        public Task RemoveDocumentAsync(string filePath)
        {
            RemoveDocument(filePath);
            return Task.CompletedTask;
        }

        /// <summary>
        /// 清空所有向量索引
        /// </summary>
        public Task ClearAllAsync()
        {
            _database.Chunks.Clear();
            SaveDatabase();
            return Task.CompletedTask;
        }

        /// <summary>
        /// 文本分块
        /// </summary>
        private List<string> ChunkText(string text, int chunkSize = 500, int overlap = 50)
        {
            var chunks = new List<string>();
            
            if (string.IsNullOrWhiteSpace(text))
                return chunks;

            // 先尝试按双换行分割，如果没有就按单换行分割
            var paragraphs = text.Split(new[] { "\n\n", "\r\n\r\n" }, StringSplitOptions.RemoveEmptyEntries);
            
            // 如果只有一个段落，尝试按单换行分割
            if (paragraphs.Length <= 1)
            {
                paragraphs = text.Split(new[] { "\n", "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
            }
            
            // 如果还是没有分割开，且文本足够长，按字符数强制分块
            if (paragraphs.Length <= 1 && text.Length > chunkSize)
            {
                for (int i = 0; i < text.Length; i += chunkSize - overlap)
                {
                    var length = Math.Min(chunkSize, text.Length - i);
                    chunks.Add(text.Substring(i, length).Trim());
                }
                return chunks;
            }
            
            // 如果文本很短，直接作为一个块
            if (paragraphs.Length <= 1 && text.Length <= chunkSize)
            {
                var trimmed = text.Trim();
                if (!string.IsNullOrWhiteSpace(trimmed))
                {
                    chunks.Add(trimmed);
                }
                return chunks;
            }
            
            var currentChunk = new List<string>();
            var currentLength = 0;

            foreach (var para in paragraphs)
            {
                var trimmedPara = para.Trim();
                if (string.IsNullOrWhiteSpace(trimmedPara))
                    continue;

                // 如果当前块加上新段落超过限制，保存当前块
                if (currentLength + trimmedPara.Length > chunkSize && currentChunk.Count > 0)
                {
                    chunks.Add(string.Join("\n\n", currentChunk));
                    
                    // 保留最后一个段落作为重叠
                    if (overlap > 0 && currentChunk.Count > 0)
                    {
                        var lastPara = currentChunk.Last();
                        currentChunk.Clear();
                        if (lastPara.Length <= overlap)
                        {
                            currentChunk.Add(lastPara);
                            currentLength = lastPara.Length;
                        }
                        else
                        {
                            currentLength = 0;
                        }
                    }
                    else
                    {
                        currentChunk.Clear();
                        currentLength = 0;
                    }
                }

                currentChunk.Add(trimmedPara);
                currentLength += trimmedPara.Length;
            }

            // 保存最后一块
            if (currentChunk.Count > 0)
            {
                chunks.Add(string.Join("\n\n", currentChunk));
            }

            // 如果没有分出块（可能是单段长文本），按字符分割
            if (chunks.Count == 0 && !string.IsNullOrWhiteSpace(text))
            {
                for (int i = 0; i < text.Length; i += chunkSize - overlap)
                {
                    var length = Math.Min(chunkSize, text.Length - i);
                    chunks.Add(text.Substring(i, length));
                }
            }

            return chunks;
        }

        /// <summary>
        /// 计算余弦相似度
        /// </summary>
        private float CosineSimilarity(float[] a, float[] b)
        {
            if (a.Length != b.Length || a.Length == 0)
                return 0;

            float dotProduct = 0;
            float normA = 0;
            float normB = 0;

            for (int i = 0; i < a.Length; i++)
            {
                dotProduct += a[i] * b[i];
                normA += a[i] * a[i];
                normB += b[i] * b[i];
            }

            if (normA == 0 || normB == 0)
                return 0;

            return dotProduct / (float)(Math.Sqrt(normA) * Math.Sqrt(normB));
        }

        private VectorDatabase LoadDatabase()
        {
            try
            {
                if (File.Exists(_storePath))
                {
                    var json = File.ReadAllText(_storePath);
                    return JsonConvert.DeserializeObject<VectorDatabase>(json) ?? new VectorDatabase();
                }
            }
            catch
            {
                // 加载失败，返回新数据库
            }
            return new VectorDatabase();
        }

        private void SaveDatabase()
        {
            try
            {
                var dir = Path.GetDirectoryName(_storePath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                var json = JsonConvert.SerializeObject(_database, Formatting.Indented);
                File.WriteAllText(_storePath, json);
            }
            catch
            {
                // 保存失败，忽略
            }
        }
    }

    /// <summary>
    /// 向量数据库
    /// </summary>
    public class VectorDatabase
    {
        public List<DocumentChunk> Chunks { get; set; } = new();
    }

    /// <summary>
    /// 文档块
    /// </summary>
    public class DocumentChunk
    {
        public string Id { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public int ChunkIndex { get; set; }
        public string Content { get; set; } = string.Empty;
        public float[] Embedding { get; set; } = Array.Empty<float>();
    }

    /// <summary>
    /// 搜索结果
    /// </summary>
    public class SearchResult
    {
        public DocumentChunk Chunk { get; set; } = null!;
        public float Score { get; set; }
    }
}
