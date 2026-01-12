namespace LocalKnowledgeBase.Models
{
    /// <summary>
    /// 向量索引项（用于 UI 显示）
    /// </summary>
    public class VectorIndexItem
    {
        public string FilePath { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public int ChunkCount { get; set; }
    }
}
