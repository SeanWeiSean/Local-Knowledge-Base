using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using LocalKnowledgeBase.Models;
using Newtonsoft.Json;

namespace LocalKnowledgeBase.Services
{
    /// <summary>
    /// 聊天服务实现 - 对接Ollama ChatCompletion API
    /// </summary>
    public class ChatService : IChatService
    {
        private readonly HttpClient _httpClient;
        private string _endpoint;
        private string _modelName;

        public string ModelName
        {
            get => _modelName;
            set => _modelName = value;
        }

        public string Endpoint
        {
            get => _endpoint;
            set => _endpoint = value;
        }

        public ChatService()
        {
            _httpClient = new HttpClient();
            _httpClient.Timeout = TimeSpan.FromMinutes(5);

            // 从配置文件读取设置
            _endpoint = ConfigurationManager.AppSettings["OllamaEndpoint"] ?? "http://localhost:11434/api/chat";
            _modelName = ConfigurationManager.AppSettings["ModelName"] ?? "qwen3:1.7b";
        }

        public async Task<string> GenerateSummaryAsync(string text)
        {
            // 限制文本长度，避免超过模型限制
            if (text.Length > 8000)
            {
                text = text.Substring(0, 8000) + "...";
            }

            var prompt = $@"请用段落格式总结以下文档，先写总体概述，再分点说明细节：

{text}

要求：
1. 第一段：总体概述文档主题
2. 后续段落：分点展开关键内容
3. 使用自然段落，不要列表符号";

            var request = new ChatCompletionRequest
            {
                model = _modelName,
                messages = new List<ChatCompletionMessage>
                {
                    new ChatCompletionMessage
                    {
                        role = "system",
                        content = "你是文档摘要助手。用段落格式总结，先总后分。"
                    },
                    new ChatCompletionMessage
                    {
                        role = "user",
                        content = prompt
                    }
                },
                stream = false,
                temperature = 0.3
            };

            return await SendChatRequestAsync(request);
        }

        public async Task<string> AskQuestionAsync(string question, string context)
        {
            // 限制上下文长度
            if (context.Length > 6000)
            {
                context = context.Substring(0, 6000) + "...";
            }

            string prompt;
            string systemPrompt;

            if (string.IsNullOrWhiteSpace(context))
            {
                // 普通对话模式
                prompt = question;
                systemPrompt = "你是一个智能助手，用中文回答用户的问题。";
            }
            else
            {
                // 知识库模式
                prompt = $@"基于以下知识库内容回答用户的问题。

知识库内容：
{context}

用户问题：
{question}

请根据知识库内容用中文回答问题。如果知识库中没有相关信息，请明确告知用户。";
                systemPrompt = "你是一个智能问答助手，根据提供的知识库内容准确回答用户的问题。请用中文回答。";
            }

            var request = new ChatCompletionRequest
            {
                model = _modelName,
                messages = new List<ChatCompletionMessage>
                {
                    new ChatCompletionMessage
                    {
                        role = "system",
                        content = systemPrompt
                    },
                    new ChatCompletionMessage
                    {
                        role = "user",
                        content = prompt
                    }
                },
                stream = false,
                temperature = 0.7
            };

            return await SendChatRequestAsync(request);
        }

        public async Task AskQuestionStreamAsync(string question, string context, Action<string, ChatCompletionResponse?> onChunk)
        {
            // 限制上下文长度
            if (context.Length > 6000)
            {
                context = context.Substring(0, 6000) + "...";
            }

            string prompt;
            string systemPrompt;

            if (string.IsNullOrWhiteSpace(context))
            {
                prompt = question;
                systemPrompt = "你是一个智能助手，用中文回答用户的问题。";
            }
            else
            {
                prompt = $@"基于以下知识库内容回答用户的问题。

知识库内容：
{context}

用户问题：
{question}

请根据知识库内容用中文回答问题。如果知识库中没有相关信息，请明确告知用户。";
                systemPrompt = "你是一个智能问答助手，根据提供的知识库内容准确回答用户的问题。请用中文回答。";
            }

            var request = new ChatCompletionRequest
            {
                model = _modelName,
                messages = new List<ChatCompletionMessage>
                {
                    new ChatCompletionMessage
                    {
                        role = "system",
                        content = systemPrompt
                    },
                    new ChatCompletionMessage
                    {
                        role = "user",
                        content = prompt
                    }
                },
                stream = true,
                temperature = 0.7
            };

            await SendChatRequestStreamAsync(request, onChunk);
        }

        public async Task<string> CompressConversationAsync(string conversationHistory)
        {
            var prompt = $@"请用简洁的语言总结以下对话，保留关键信息：

{conversationHistory}

要求：
1. 用1-2句话总结每个问答的核心
2. 只保留关键信息
3. 不要列表，用自然段落";

            var request = new ChatCompletionRequest
            {
                model = _modelName,
                messages = new List<ChatCompletionMessage>
                {
                    new ChatCompletionMessage
                    {
                        role = "system",
                        content = "你是对话压缩助手。用极简语言总结对话历史。"
                    },
                    new ChatCompletionMessage
                    {
                        role = "user",
                        content = prompt
                    }
                },
                stream = false,
                temperature = 0.3
            };

            return await SendChatRequestAsync(request);
        }

        private async Task<string> SendChatRequestAsync(ChatCompletionRequest request)
        {
            try
            {
                var json = JsonConvert.SerializeObject(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(_endpoint, content);
                response.EnsureSuccessStatusCode();

                var responseBody = await response.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<ChatCompletionResponse>(responseBody);

                return result?.message?.content ?? "抱歉，没有收到有效的回复。";
            }
            catch (HttpRequestException ex)
            {
                throw new Exception($"连接到AI服务失败。请确保Ollama服务正在运行。错误: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                throw new Exception($"AI请求失败: {ex.Message}", ex);
            }
        }

        private async Task SendChatRequestStreamAsync(ChatCompletionRequest request, Action<string, ChatCompletionResponse?> onChunk)
        {
            try
            {
                var json = JsonConvert.SerializeObject(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var httpRequest = new HttpRequestMessage(HttpMethod.Post, _endpoint)
                {
                    Content = content
                };

                using var response = await _httpClient.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead);
                response.EnsureSuccessStatusCode();

                using var stream = await response.Content.ReadAsStreamAsync();
                using var reader = new StreamReader(stream, Encoding.UTF8, false, 1024, true);

                string? line;
                while ((line = await reader.ReadLineAsync()) != null)
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;

                    try
                    {
                        var chunk = JsonConvert.DeserializeObject<ChatCompletionResponse>(line);
                        if (chunk != null)
                        {
                            var text = chunk.message?.content ?? "";
                            onChunk(text, chunk.done ? chunk : null);
                        }
                    }
                    catch (JsonException)
                    {
                        // 忽略解析错误的行
                    }
                }
            }
            catch (HttpRequestException ex)
            {
                throw new Exception($"连接到AI服务失败。请确保Ollama服务正在运行。错误: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                throw new Exception($"AI请求失败: {ex.Message}", ex);
            }
        }
    }
}
