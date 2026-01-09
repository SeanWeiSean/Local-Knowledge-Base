using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using OfficeOpenXml;
using LocalKnowledgeBase.Models;
using DocType = LocalKnowledgeBase.Models.DocumentType;

namespace LocalKnowledgeBase.Services
{
    /// <summary>
    /// 文档处理服务实现
    /// </summary>
    public class DocumentService : IDocumentService
    {
        public DocumentService()
        {
            // 设置EPPlus许可证上下文
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        }

        public async Task<string> ExtractTextAsync(string filePath, DocType type)
        {
            return await Task.Run(() =>
            {
                try
                {
                    switch (type)
                    {
                        case DocType.Word:
                            return ExtractWordText(filePath);
                        case DocType.Excel:
                            return ExtractExcelText(filePath);
                        case DocType.Folder:
                            return $"文件夹: {Path.GetFileName(filePath)}";
                        default:
                            throw new NotSupportedException($"不支持的文档类型: {type}");
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception($"提取文本失败: {ex.Message}", ex);
                }
            });
        }

        public async Task<string> SaveSummaryAsync(string originalFilePath, string summary)
        {
            return await Task.Run(() =>
            {
                try
                {
                    var directory = Path.GetDirectoryName(originalFilePath);
                    var fileNameWithoutExt = Path.GetFileNameWithoutExtension(originalFilePath);
                    var summaryFileName = $"{fileNameWithoutExt}_summary.txt";
                    var summaryPath = Path.Combine(directory!, summaryFileName);

                    File.WriteAllText(summaryPath, summary, Encoding.UTF8);
                    return summaryPath;
                }
                catch (Exception ex)
                {
                    throw new Exception($"保存摘要失败: {ex.Message}", ex);
                }
            });
        }

        private string ExtractWordText(string filePath)
        {
            var text = new StringBuilder();

            using (WordprocessingDocument wordDoc = WordprocessingDocument.Open(filePath, false))
            {
                var body = wordDoc.MainDocumentPart?.Document?.Body;
                if (body != null)
                {
                    foreach (var paragraph in body.Descendants<Paragraph>())
                    {
                        text.AppendLine(paragraph.InnerText);
                    }
                }
            }

            return text.ToString();
        }

        private string ExtractExcelText(string filePath)
        {
            var text = new StringBuilder();

            using (var package = new ExcelPackage(new FileInfo(filePath)))
            {
                foreach (var worksheet in package.Workbook.Worksheets)
                {
                    text.AppendLine($"工作表: {worksheet.Name}");
                    text.AppendLine(new string('-', 50));

                    var start = worksheet.Dimension?.Start;
                    var end = worksheet.Dimension?.End;

                    if (start != null && end != null)
                    {
                        for (int row = start.Row; row <= end.Row; row++)
                        {
                            var rowData = new StringBuilder();
                            for (int col = start.Column; col <= end.Column; col++)
                            {
                                var cell = worksheet.Cells[row, col];
                                if (cell.Value != null)
                                {
                                    rowData.Append(cell.Value.ToString());
                                    rowData.Append("\t");
                                }
                            }
                            if (rowData.Length > 0)
                            {
                                text.AppendLine(rowData.ToString());
                            }
                        }
                    }

                    text.AppendLine();
                }
            }

            return text.ToString();
        }
    }
}
