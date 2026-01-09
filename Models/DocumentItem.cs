using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace LocalKnowledgeBase.Models
{
    /// <summary>
    /// 知识库文档模型
    /// </summary>
    public class DocumentItem : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string FilePath { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public DateTime AddedTime { get; set; } = DateTime.Now;

        private bool _isSummarized = false;
        public bool IsSummarized
        {
            get => _isSummarized;
            set
            {
                if (_isSummarized != value)
                {
                    _isSummarized = value;
                    OnPropertyChanged();
                }
            }
        }

        public string? SummaryPath { get; set; }
        public string? Summary { get; set; }
        public DocumentType Type { get; set; }
    }

    public enum DocumentType
    {
        Word,
        Excel,
        Folder
    }
}
