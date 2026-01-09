using System;

namespace LocalKnowledgeBase.Models
{
    /// <summary>
    /// 聊天消息模型
    /// </summary>
    public class ChatMessage
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Content { get; set; } = string.Empty;
        public MessageRole Role { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public bool IsLoading { get; set; } = false;
    }

    public enum MessageRole
    {
        User,
        Assistant,
        System
    }
}
