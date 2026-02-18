using System.Globalization;
using System.Text;
using Oikos.Application.Services.Invoice;
using Oikos.Application.Services.Invoice.Models;
using Oikos.Infrastructure.Constants;
using PdfSharpCore.Pdf;
using PdfSharpCore.Pdf.IO;
using PdfSharpCore.Pdf.IO.enums;

namespace Oikos.Infrastructure.Services.Invoice;

public class PowerOfAttorneyPdfGenerator : IPowerOfAttorneyPdfGenerator
{
    public byte[] Generate(PowerOfAttorneyPdfDetails details, string templatePath)
    {
        if (details is null)
        {
            return Array.Empty<byte>();
        }

        if (string.IsNullOrWhiteSpace(templatePath) || !File.Exists(templatePath))
        {
            return Array.Empty<byte>();
        }

        try
        {
            using var inputStream = File.OpenRead(templatePath);
            using var document = PdfReader.Open(
                inputStream,
                password: null,
                openmode: PdfDocumentOpenMode.Modify,
                accuracy: PdfReadAccuracy.Moderate);
            if (document.PageCount == 0)
            {
                return Array.Empty<byte>();
            }

            var page = document.Pages[0];
            var pageHeight = page.Height.Point;

            var signerName = BuildSignerName(details);
            var addressLine = BuildAddressLine(details);
            var placeDateLine = BuildPlaceDateLine(details);
            var signature = details.Signature?.Trim() ?? details.CreditorName.Trim();

            EnsureHelveticaFontResource(document, page);

            var entries = new List<TextEntry>
            {
                new(signerName, PowerOfAttorneyPdfConstants.NameX, pageHeight - PowerOfAttorneyPdfConstants.NameY),
                new(addressLine, PowerOfAttorneyPdfConstants.AddressX, pageHeight - PowerOfAttorneyPdfConstants.AddressY),
                new(placeDateLine, PowerOfAttorneyPdfConstants.PlaceDateX, pageHeight - PowerOfAttorneyPdfConstants.PlaceDateY),
                new(signature, PowerOfAttorneyPdfConstants.SignatureX, pageHeight - PowerOfAttorneyPdfConstants.SignatureY)
            };

            // Debug logging
            Console.WriteLine($"PDF Generator - Signer: {signerName}");
            Console.WriteLine($"PDF Generator - Address: {addressLine}");
            Console.WriteLine($"PDF Generator - Date: {placeDateLine}");
            Console.WriteLine($"PDF Generator - Signature: {signature}");

            AppendTextContent(document, page, entries);

            using var outputStream = new MemoryStream();
            document.Save(outputStream, false);
            return outputStream.ToArray();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"PDF Generator Exception: {ex.Message}");
            return Array.Empty<byte>();
        }
    }

    private static string BuildSignerName(PowerOfAttorneyPdfDetails details)
    {
        if (!string.IsNullOrWhiteSpace(details.CreditorName))
        {
            return details.CreditorName.Trim();
        }

        return string.Empty;
    }

    private static string BuildPlaceDateLine(PowerOfAttorneyPdfDetails details)
    {
        return details.SignatureDate.ToLocalTime().ToString("dd.MM.yyyy");
    }

    private static string BuildAddressLine(PowerOfAttorneyPdfDetails details)
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(details.DebtorStreet))
        {
            parts.Add(details.DebtorStreet.Trim());
        }

        var cityParts = new List<string>();
        if (!string.IsNullOrWhiteSpace(details.DebtorPostalCode))
        {
            cityParts.Add(details.DebtorPostalCode.Trim());
        }

        if (!string.IsNullOrWhiteSpace(details.DebtorCity))
        {
            cityParts.Add(details.DebtorCity.Trim());
        }

        if (cityParts.Count > 0)
        {
            parts.Add(string.Join(" ", cityParts));
        }

        return parts.Count == 0 ? string.Empty : string.Join(", ", parts);
    }

    private static void EnsureHelveticaFontResource(PdfDocument document, PdfPage page)
    {
        var resources = page.Elements.GetDictionary("/Resources");
        if (resources == null)
        {
            resources = new PdfDictionary(document);
            page.Elements["/Resources"] = resources;
        }

        var fonts = resources.Elements.GetDictionary("/Font");
        if (fonts == null)
        {
            fonts = new PdfDictionary(document);
            resources.Elements["/Font"] = fonts;
        }

        if (!fonts.Elements.ContainsKey("/F1"))
        {
            var font = new PdfDictionary(document);
            font.Elements["/Type"] = new PdfName("/Font");
            font.Elements["/Subtype"] = new PdfName("/Type1");
            font.Elements["/BaseFont"] = new PdfName("/Helvetica");
            document.Internals.AddObject(font);
            fonts.Elements["/F1"] = font.Reference;
        }
    }

    private static void AppendTextContent(PdfDocument document, PdfPage page, IEnumerable<TextEntry> entries)
    {
        var content = BuildContentStream(entries);
        if (string.IsNullOrWhiteSpace(content))
        {
            return;
        }

        var contentBytes = Encoding.ASCII.GetBytes(content);
        var stream = new PdfDictionary(document);
        stream.CreateStream(contentBytes);
        document.Internals.AddObject(stream);
        page.Contents.Elements.Add(stream.Reference);
    }

    private static string BuildContentStream(IEnumerable<TextEntry> entries)
    {
        var content = new StringBuilder();
        content.AppendLine("BT");
        content.AppendLine("/F1 12 Tf");

        foreach (var entry in entries)
        {
            if (string.IsNullOrWhiteSpace(entry.Text))
            {
                continue;
            }

            content.AppendFormat(CultureInfo.InvariantCulture, "1 0 0 1 {0} {1} Tm\n", entry.X, entry.Y);
            content.Append('(');
            content.Append(SanitizeText(entry.Text));
            content.AppendLine(") Tj");
        }

        content.AppendLine("ET");
        return content.ToString();
    }

    private static string SanitizeText(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var sanitized = new StringBuilder(value.Length);
        foreach (var character in value)
        {
            if (character == '(' || character == ')' || character == '\\')
            {
                sanitized.Append(' ');
                continue;
            }

            if (character < 32 || character > 126)
            {
                sanitized.Append(' ');
                continue;
            }

            sanitized.Append(character);
        }

        return sanitized.ToString();
    }

    private sealed record TextEntry(string Text, double X, double Y);
}
