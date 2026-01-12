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

        private bool _isIndexed = false;
        public bool IsIndexed
        {
            get => _isIndexed;
            set
            {
                if (_isIndexed != value)
                {
                    _isIndexed = value;
                    OnPropertyChanged();
                }
            }
        }

        private int _chunkCount = 0;
        public int ChunkCount
        {
            get => _chunkCount;
            set
            {
                if (_chunkCount != value)
                {
                    _chunkCount = value;
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
        PowerPoint,
        Folder
    }
}
