using System.Text;
using Oikos.Application.Services.CompanyCheck;
using Oikos.Application.Services.CompanyCheck.Models;

namespace Oikos.Infrastructure.CompanyCheck;

public class SepaMandateGenerator : ISepaMandateGenerator
{
    public byte[] Generate(SepaMandateDetails details)
    {
        if (details == null)
        {
            return Array.Empty<byte>();
        }

        var lines = BuildContentLines(details);
        if (lines.Count == 0)
        {
            return Array.Empty<byte>();
        }

        var pdfBytes = BuildSimplePdf(lines);
        return pdfBytes;
    }

    private static List<string> BuildContentLines(SepaMandateDetails details)
    {
        var lines = new List<string>
        {
            "SEPA-Firmenlastschriftmandat",
            string.Empty,
            "Gläubiger: Oikos Holding GmbH – Marke Rechtfix",
            "Langestraße 75",
            "76530 Baden-Baden",
            "Deutschland",
            "Gläubiger-ID: DE40ZZZ00002830194",
            "Mandatsreferenz: Mitgliedschaft Rechtfix",
            string.Empty,
            "Angaben zum Zahlungspflichtigen:",
            $"Firma / Name: {details.CompanyName}",
            $"Rechtsform: {details.LegalForm}",
            $"Vertretungsberechtigte Person: {details.AuthorizedRepresentative}",
            $"Straße / Nr.: {details.AddressLine1}",
            $"PLZ / Ort: {details.PostalCode} {details.City}",
            $"Land: {details.Country}",
            $"E-Mail: {details.Email}",
            $"IBAN: {details.Iban}",
            $"BIC: {details.Bic}",
            $"Kreditinstitut: {details.BankName}",
            $"Kontoinhaber: {details.AccountHolderName}",
            string.Empty,
            $"Zahlungsart: Wiederkehrende Zahlung (SEPA B2B)",
            $"Zahlungszweck: {details.PaymentPurpose}",
            $"Zahlungsintervall: {details.PaymentInterval}",
            string.Empty,
            "Bestätigung zur Weiterleitung an die Bank: Ich erlaube der Oikos Holding GmbH – Marke Rechtfix, dieses SEPA-Firmenlastschriftmandat in meinem Namen an meine Bank weiterzuleiten.",
            $"Ort: {details.SignatureCity}",
            $"Datum: {(details.SignatureDate ?? DateTime.UtcNow.Date):dd.MM.yyyy}"
        };

        return lines;
    }

    private static byte[] BuildSimplePdf(IReadOnlyList<string> lines)
    {
        var sanitizedLines = new List<string>(lines.Count);
        foreach (var line in lines)
        {
            sanitizedLines.Add(SanitizeText(line));
        }

        var contentStream = BuildContentStream(sanitizedLines);
        var objects = new List<string>
        {
            "1 0 obj\n<< /Type /Catalog /Pages 2 0 R >>\nendobj\n",
            "2 0 obj\n<< /Type /Pages /Kids [3 0 R] /Count 1 >>\nendobj\n",
            "3 0 obj\n<< /Type /Page /Parent 2 0 R /MediaBox [0 0 595 842] /Contents 4 0 R /Resources << /Font << /F1 5 0 R >> >> >>\nendobj\n",
            $"4 0 obj\n<< /Length {Encoding.ASCII.GetByteCount(contentStream)} >>\nstream\n{contentStream}endstream\nendobj\n",
            "5 0 obj\n<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>\nendobj\n"
        };

        var builder = new StringBuilder();
        builder.Append("%PDF-1.4\n");

        var offsets = new List<int> { 0 };
        for (var i = 0; i < objects.Count; i++)
        {
            offsets.Add(builder.Length);
            builder.Append(objects[i]);
        }

        var xrefPosition = builder.Length;
        builder.Append("xref\n0 6\n0000000000 65535 f \n");
        for (var i = 1; i <= 5; i++)
        {
            builder.AppendFormat("{0:0000000000} 00000 n \n", offsets[i]);
        }

        builder.Append("trailer\n<< /Size 6 /Root 1 0 R >>\nstartxref\n");
        builder.Append(xrefPosition);
        builder.Append("\n%%EOF");

        return Encoding.ASCII.GetBytes(builder.ToString());
    }

    private static string BuildContentStream(IReadOnlyList<string> lines)
    {
        var content = new StringBuilder();
        content.AppendLine("BT");
        content.AppendLine("/F1 12 Tf");
        content.AppendLine("1 0 0 1 50 800 Tm");

        foreach (var line in lines)
        {
            content.Append('(');
            content.Append(line);
            content.AppendLine(") Tj");
            content.AppendLine("0 -16 Td");
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
}
