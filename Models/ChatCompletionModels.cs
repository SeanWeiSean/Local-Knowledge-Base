using System.Collections.Generic;

namespace LocalKnowledgeBase.Models
{
    /// <summary>
    /// ChatCompletion API 请求模型
    /// </summary>
    public class ChatCompletionRequest
    {
        public string model { get; set; } = string.Empty;
        public List<ChatCompletionMessage> messages { get; set; } = new();
        public bool stream { get; set; } = false;
        public double temperature { get; set; } = 0.7;
    }

    public class ChatCompletionMessage
    {
        public string role { get; set; } = string.Empty;
        public string content { get; set; } = string.Empty;
    }

    /// <summary>
    /// ChatCompletion API 响应模型
    /// </summary>
    public class ChatCompletionResponse
    {
        public string model { get; set; } = string.Empty;
        public string created_at { get; set; } = string.Empty;
        public ChatCompletionMessage? message { get; set; }
        public bool done { get; set; }
        
        // Token 统计 (Ollama 在 done=true 时返回，值很大需要用 long)
        public long total_duration { get; set; }
        public long load_duration { get; set; }
        public long prompt_eval_count { get; set; }
        public long prompt_eval_duration { get; set; }
        public long eval_count { get; set; }
        public long eval_duration { get; set; }
    }
}
