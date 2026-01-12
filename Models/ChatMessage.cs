using System;

namespace LocalKnowledgeBase.Models
{
    /// <summary>
    /// 聊天消息模型
    /// </summary>
    public class ChatMessage : LocalKnowledgeBase.ViewModels.ViewModelBase
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        
        private string _content = string.Empty;
        public string Content 
        { 
            get => _content;
            set => SetProperty(ref _content, value);
        }
        
        public MessageRole Role { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.Now;
        
        private bool _isLoading = false;
        public bool IsLoading 
        { 
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        private string _tokenStats = string.Empty;
        public string TokenStats
        {
            get => _tokenStats;
            set => SetProperty(ref _tokenStats, value);
        }
    }

    public enum MessageRole
    {
        User,
        Assistant,
        System
    }
}
