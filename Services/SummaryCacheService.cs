using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace LocalKnowledgeBase.Services
{
    /// <summary>
    /// 摘要缓存服务，用于持久化已生成的文档摘要
    /// </summary>
    public class SummaryCacheService
    {
        private readonly string _cacheFilePath;
        private Dictionary<string, CachedSummary> _cache;

        public SummaryCacheService()
        {
            var appDataPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "LocalKnowledgeBase"
            );
            Directory.CreateDirectory(appDataPath);
            _cacheFilePath = Path.Combine(appDataPath, "summary_cache.json");
            _cache = LoadCache();
        }

        /// <summary>
        /// 检查文档是否已有缓存的摘要
        /// </summary>
        public bool HasCachedSummary(string filePath)
        {
            return _cache.ContainsKey(filePath);
        }

        /// <summary>
        /// 获取缓存的摘要
        /// </summary>
        public CachedSummary? GetCachedSummary(string filePath)
        {
            return _cache.TryGetValue(filePath, out var summary) ? summary : null;
        }

        /// <summary>
        /// 保存摘要到缓存
        /// </summary>
        public void SaveSummary(string filePath, string summary, string summaryPath)
        {
            _cache[filePath] = new CachedSummary
            {
                FilePath = filePath,
                Summary = summary,
                SummaryPath = summaryPath,
                GeneratedTime = DateTime.Now
            };
            SaveCache();
        }

        /// <summary>
        /// 从缓存中移除摘要
        /// </summary>
        public void RemoveSummary(string filePath)
        {
            if (_cache.ContainsKey(filePath))
            {
                _cache.Remove(filePath);
                SaveCache();
            }
        }

        /// <summary>
        /// 从磁盘加载缓存
        /// </summary>
        private Dictionary<string, CachedSummary> LoadCache()
        {
            try
            {
                if (File.Exists(_cacheFilePath))
                {
                    var json = File.ReadAllText(_cacheFilePath);
                    var cache = JsonSerializer.Deserialize<Dictionary<string, CachedSummary>>(json);
                    return cache ?? new Dictionary<string, CachedSummary>();
                }
            }
            catch (Exception)
            {
                // 如果加载失败，返回空字典
            }

            return new Dictionary<string, CachedSummary>();
        }

        /// <summary>
        /// 保存缓存到磁盘
        /// </summary>
        private void SaveCache()
        {
            try
            {
                var json = JsonSerializer.Serialize(_cache, new JsonSerializerOptions
                {
                    WriteIndented = true
                });
                File.WriteAllText(_cacheFilePath, json);
            }
            catch (Exception)
            {
                // 保存失败时静默处理
            }
        }

        /// <summary>
        /// 清理不存在的文件的缓存（可选功能）
        /// </summary>
        public void CleanupNonExistentFiles()
        {
            var keysToRemove = new List<string>();
            foreach (var kvp in _cache)
            {
                if (!File.Exists(kvp.Key))
                {
                    keysToRemove.Add(kvp.Key);
                }
            }

            foreach (var key in keysToRemove)
            {
                _cache.Remove(key);
            }

            if (keysToRemove.Count > 0)
            {
                SaveCache();
            }
        }
    }

    /// <summary>
    /// 缓存的摘要信息
    /// </summary>
    public class CachedSummary
    {
        public string FilePath { get; set; } = string.Empty;
        public string Summary { get; set; } = string.Empty;
        public string SummaryPath { get; set; } = string.Empty;
        public DateTime GeneratedTime { get; set; }
    }
}
