using System;
using System.Collections.Generic;
using System.Configuration;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace LocalKnowledgeBase.Services
{
    /// <summary>
    /// Embedding 服务 - 使用 Ollama 的 embedding API
    /// </summary>
    public class EmbeddingService
    {
        private readonly HttpClient _httpClient;
        private string _endpoint;
        private string _modelName;

        public string ModelName
        {
            get => _modelName;
            set => _modelName = value;
        }

        public EmbeddingService()
        {
            _httpClient = new HttpClient();
            _httpClient.Timeout = TimeSpan.FromMinutes(5);

            // Ollama embedding endpoint
            var baseUrl = ConfigurationManager.AppSettings["OllamaEndpoint"] ?? "http://localhost:11434/api/chat";
            _endpoint = baseUrl.Replace("/api/chat", "/api/embeddings");
            _modelName = ConfigurationManager.AppSettings["EmbeddingModel"] ?? "nomic-embed-text";
        }

        /// <summary>
        /// 获取文本的向量表示
        /// </summary>
        public async Task<float[]> GetEmbeddingAsync(string text)
        {
            try
            {
                var request = new
                {
                    model = _modelName,
                    prompt = text
                };

                var json = JsonConvert.SerializeObject(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(_endpoint, content);
                response.EnsureSuccessStatusCode();

                var responseBody = await response.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<EmbeddingResponse>(responseBody);

                return result?.embedding ?? Array.Empty<float>();
            }
            catch (Exception ex)
            {
                throw new Exception($"获取 Embedding 失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 批量获取多个文本的向量
        /// </summary>
        public async Task<List<float[]>> GetEmbeddingsAsync(List<string> texts)
        {
            var embeddings = new List<float[]>();
            foreach (var text in texts)
            {
                var embedding = await GetEmbeddingAsync(text);
                embeddings.Add(embedding);
            }
            return embeddings;
        }
    }

    public class EmbeddingResponse
    {
        public float[] embedding { get; set; } = Array.Empty<float>();
    }
}
