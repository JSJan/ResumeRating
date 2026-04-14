using System.Text;
using DocumentFormat.OpenXml.Packaging;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf.Canvas.Parser.Listener;

namespace ResumeRating.Api.Services;

public interface IResumeParserService
{
    Task<string> ExtractTextAsync(Stream fileStream, string fileName);
}

public class ResumeParserService : IResumeParserService
{
    public Task<string> ExtractTextAsync(Stream fileStream, string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        return extension switch
        {
            ".pdf" => Task.FromResult(ExtractFromPdf(fileStream)),
            ".docx" => Task.FromResult(ExtractFromDocx(fileStream)),
            ".txt" => ExtractFromTxt(fileStream),
            ".doc" => throw new NotSupportedException("Legacy .doc format is not supported. Please convert to .docx."),
            _ => throw new NotSupportedException($"File format '{extension}' is not supported. Use PDF, DOCX, or TXT.")
        };
    }

    private static string ExtractFromPdf(Stream stream)
    {
        var sb = new StringBuilder();
        using var pdfReader = new PdfReader(stream);
        using var pdfDoc = new PdfDocument(pdfReader);

        for (int i = 1; i <= pdfDoc.GetNumberOfPages(); i++)
        {
            var page = pdfDoc.GetPage(i);
            var strategy = new SimpleTextExtractionStrategy();
            var text = PdfTextExtractor.GetTextFromPage(page, strategy);
            sb.AppendLine(text);
        }

        return sb.ToString().Trim();
    }

    private static string ExtractFromDocx(Stream stream)
    {
        using var doc = WordprocessingDocument.Open(stream, false);
        var body = doc.MainDocumentPart?.Document?.Body;
        return body?.InnerText ?? string.Empty;
    }

    private static async Task<string> ExtractFromTxt(Stream stream)
    {
        using var reader = new StreamReader(stream, Encoding.UTF8);
        return await reader.ReadToEndAsync();
    }
}
