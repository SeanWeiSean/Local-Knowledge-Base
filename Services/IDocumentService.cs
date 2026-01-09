using System.Threading.Tasks;
using LocalKnowledgeBase.Models;

namespace LocalKnowledgeBase.Services
{
    /// <summary>
    /// 文档处理服务接口
    /// </summary>
    public interface IDocumentService
    {
        /// <summary>
        /// 从文档中提取文本
        /// </summary>
        Task<string> ExtractTextAsync(string filePath, DocumentType type);

        /// <summary>
        /// 保存摘要到文件
        /// </summary>
        Task<string> SaveSummaryAsync(string originalFilePath, string summary);
    }
}
