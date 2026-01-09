using System.Threading.Tasks;

namespace LocalKnowledgeBase.Services
{
    /// <summary>
    /// 聊天服务接口
    /// </summary>
    public interface IChatService
    {
        /// <summary>
        /// 模型名称
        /// </summary>
        string ModelName { get; set; }

        /// <summary>
        /// API端点
        /// </summary>
        string Endpoint { get; set; }

        /// <summary>
        /// 生成文档摘要
        /// </summary>
        Task<string> GenerateSummaryAsync(string text);

        /// <summary>
        /// 根据上下文回答问题
        /// </summary>
        Task<string> AskQuestionAsync(string question, string context);

        /// <summary>
        /// 压缩对话历史
        /// </summary>
        Task<string> CompressConversationAsync(string conversationHistory);
    }
}
