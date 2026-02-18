using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using UglyToad.PdfPig;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;
using Oikos.Application.Services.Invoice;
using Oikos.Application.Services.Invoice.Models;

namespace Oikos.Infrastructure.Services.Invoice;

public sealed class HeuristicInvoiceExtractionService : IInvoiceExtractionService
{
    private readonly ILogger<HeuristicInvoiceExtractionService> _logger;

    public HeuristicInvoiceExtractionService(ILogger<HeuristicInvoiceExtractionService> logger)
    {
        _logger = logger;
    }

    public Task<InvoiceAiExtractionResult?> ExtractAsync(
        InvoiceAiExtractionRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        var text = string.IsNullOrWhiteSpace(request.RawText)
            ? TryExtractTextFromPdf(request.PdfData, cancellationToken)
            : request.RawText;

        if (string.IsNullOrWhiteSpace(text))
        {
            return Task.FromResult<InvoiceAiExtractionResult?>(null);
        }

        var parser = new HeuristicInvoiceParser(text!, request.Culture, request.FileName);
        var result = parser.Parse();

        if (result is null || !(result.HasInvoiceValues || result.HasDebtorValues))
        {
            return Task.FromResult<InvoiceAiExtractionResult?>(null);
        }

        return Task.FromResult<InvoiceAiExtractionResult?>(result);
    }

    private string? TryExtractTextFromPdf(byte[]? pdfData, CancellationToken cancellationToken)
    {
        if (pdfData is not { Length: > 0 })
        {
            return null;
        }

        try
        {
            using var stream = new MemoryStream(pdfData, writable: false);
            using var document = PdfDocument.Open(stream);
            var builder = new StringBuilder();

            foreach (var page in document.GetPages())
            {
                cancellationToken.ThrowIfCancellationRequested();
                builder.AppendLine(ContentOrderTextExtractor.GetText(page));
            }

            return builder.ToString();
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to extract text from invoice PDF.");
            return null;
        }
    }

    private static string? NormalizeCurrency(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        if (trimmed.Length == 1)
        {
            return trimmed switch
            {
                "€" => "EUR",
                "$" => "USD",
                "£" => "GBP",
                _ => trimmed
            };
        }

        var letters = new string(trimmed.Where(char.IsLetter).ToArray());
        if (letters.Length == 3)
        {
            return letters.ToUpperInvariant();
        }

        return trimmed.ToUpperInvariant();
    }

    private static string? Sanitize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        return trimmed.Length == 0 ? null : trimmed;
    }

    private static string? PreferValue(string? primary, string? fallback)
    {
        var primarySanitized = Sanitize(primary);
        if (!string.IsNullOrWhiteSpace(primarySanitized))
        {
            return primarySanitized;
        }

        return Sanitize(fallback);
    }

    private sealed class HeuristicInvoiceParser
    {
        private static readonly Regex InvoiceNumberRegex = new(
            @"(?:(?:invoice|rechnung)(?:\s*(?:no\.?|number|nr\.?|#))?|rechnungsnummer|rechnung-nr\.?|invoice-id)\s*[:#-]?\s*(?<value>[A-Z0-9][A-Z0-9\-\./]*)",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex DateCandidateRegex = new(
            @"(?<date>\d{1,2}[./-]\d{1,2}[./-]\d{2,4}|\d{4}-\d{1,2}-\d{1,2})",
            RegexOptions.Compiled);

        private static readonly Regex PostalCityRegex = new(
            @"(?<postal>\b\d{4,5}\b)\s*(?<city>[A-Za-zÄÖÜäöüß\-\s]+)",
            RegexOptions.Compiled);

        private static readonly Regex EmailRegex = new(
            @"[A-Z0-9._%+-]+@[A-Z0-9.-]+\.[A-Z]{2,}",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex PhoneRegex = new(
            @"(?:(?:\+|00)\d{1,3}[\s/-]?)?(?:\(?\d+\)?[\s/-]?){2,}",
            RegexOptions.Compiled);

        private static readonly string[] DateKeywords =
        {
            "invoice date",
            "rechnungsdatum",
            "datum",
            "date"
        };

        private static readonly (string Keyword, int Weight)[] AmountKeywords =
        {
            ("zu zahlender betrag", 7),
            ("zu zahlenden betrag", 7),
            ("amount due", 7),
            ("total due", 7),
            ("total amount", 6),
            ("total", 5),
            ("gesamtbetrag", 6),
            ("gesamtsumme", 6),
            ("summe", 3),
            ("rechnungsbetrag", 12),
            ("invoice total", 7),
            ("invoice amount", 7),
            ("netto", 2),
            ("brutto", 4),
            ("balance", 5),
            ("due", 4)
        };

        private static readonly string[] StreetIndicators =
        {
            "str.",
            "straße",
            "strasse",
            "street",
            "weg",
            "allee",
            "road",
            "platz",
            "lane",
            "drive"
        };

        private static readonly string[] SupplierIndicators =
        {
            "iban",
            "bic",
            "bank",
            "ust",
            "steuernr",
            "steuer-nr",
            "gläubiger",
            "gläubiger-id",
            "finanzamt",
            "support@",
            "hrb",
            "geschäftsführer",
            "gf:",
            "kundendienst"
        };

        private static readonly string[] InvoiceMetadataKeywords =
        {
            "rechnungsdatum",
            "invoice date",
            "rechnungsnummer",
            "invoice number",
            "kundennummer",
            "customer number",
            "mandatsreferenz",
            "order number"
        };

        private readonly string _rawText;
        private readonly string? _culture;
        private readonly string? _fileName;

        public HeuristicInvoiceParser(string rawText, string? culture, string? fileName)
        {
            _rawText = rawText;
            _culture = culture;
            _fileName = fileName;
        }

        public InvoiceAiExtractionResult? Parse()
        {
            var lines = _rawText
                .Split(new[] { '\r', '\n' }, StringSplitOptions.None)
                .Select(static l => l.Trim())
                .ToList();

            if (lines.All(static l => string.IsNullOrWhiteSpace(l)))
            {
                return null;
            }

            var invoiceNumber = FindInvoiceNumber(lines);
            var (amount, currencyFromAmount) = FindTotalAmount(lines);
            var invoiceDate = FindInvoiceDate(lines);
            var currency = NormalizeCurrency(currencyFromAmount ?? FindCurrency(lines));
            var debtor = FindDebtor(lines);
            var description = BuildDescription(lines);

            return new InvoiceAiExtractionResult(
                Sanitize(invoiceNumber),
                amount,
                Sanitize(currency),
                invoiceDate,
                Sanitize(description),
                Sanitize(debtor.Company),
                Sanitize(debtor.Street),
                Sanitize(debtor.PostalCode),
                Sanitize(debtor.City),
                Sanitize(debtor.ContactName),
                Sanitize(debtor.ContactEmail),
                Sanitize(debtor.ContactPhone));
        }

        private string? FindInvoiceNumber(IReadOnlyList<string> lines)
        {
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                var match = InvoiceNumberRegex.Match(line);
                if (match.Success)
                {
                    return match.Groups["value"].Value.Trim().Trim('.');
                }
            }

            if (!string.IsNullOrWhiteSpace(_fileName))
            {
                var nameMatch = InvoiceNumberRegex.Match(_fileName);
                if (nameMatch.Success)
                {
                    return nameMatch.Groups["value"].Value.Trim();
                }
            }

            return null;
        }

        private (string? Amount, string? Currency) FindTotalAmount(IReadOnlyList<string> lines)
        {
            string? bestAmount = null;
            string? bestCurrency = null;
            var bestScore = int.MinValue;

            for (var index = 0; index < lines.Count; index++)
            {
                var line = lines[index];
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                var evaluation = EvaluateAmountLine(line);

                if (evaluation.Score <= bestScore || string.IsNullOrWhiteSpace(evaluation.Amount))
                {
                    continue;
                }

                bestAmount = evaluation.Amount;
                bestCurrency = evaluation.Currency;
                bestScore = evaluation.Score;

                if (index + 1 < lines.Count)
                {
                    var nextEvaluation = EvaluateAmountLine(lines[index + 1]);
                    if (nextEvaluation.Score > evaluation.Score && !string.IsNullOrWhiteSpace(nextEvaluation.Amount))
                    {
                        bestAmount = nextEvaluation.Amount;
                        bestCurrency = nextEvaluation.Currency;
                        bestScore = nextEvaluation.Score;
                    }
                }
            }

            return (bestAmount, bestCurrency);
        }

        private (string? Amount, string? Currency, int Score) EvaluateAmountLine(string line)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                return (null, null, 0);
            }

            var lower = line.ToLowerInvariant();
            var score = 0;

            foreach (var (keyword, weight) in AmountKeywords)
            {
                if (lower.Contains(keyword, StringComparison.Ordinal))
                {
                    score += weight;
                }
            }

            var numberMatch = ExtractAmountCandidate(line, out var currency);
            if (string.IsNullOrWhiteSpace(numberMatch))
            {
                return (null, currency, score);
            }

            if (score == 0)
            {
                score = 1;
            }

            return (numberMatch, currency, score);
        }

        private string? ExtractAmountCandidate(string line, out string? currency)
        {
            currency = null;

            var currencyCodeMatch = Regex.Match(line, "(?<code>EUR|USD|GBP|CHF|CAD|AUD|JPY|SEK|NOK|DKK|PLN|CZK|HUF|RON|TRY|RUB|CNY|INR|BRL)", RegexOptions.IgnoreCase);
            if (currencyCodeMatch.Success)
            {
                currency = currencyCodeMatch.Groups["code"].Value.ToUpperInvariant();
            }
            else if (line.Contains('€'))
            {
                currency = "EUR";
            }
            else if (line.Contains('$'))
            {
                currency = "USD";
            }
            else if (line.Contains('£'))
            {
                currency = "GBP";
            }

            var matches = Regex.Matches(line, "([0-9][0-9.,'\\s]*)");
            if (matches.Count == 0)
            {
                return null;
            }

            string? bestCandidate = null;
            decimal bestValue = 0;

            foreach (Match match in matches)
            {
                if (!match.Success)
                {
                    continue;
                }

                var value = NormalizeAmountCandidate(match.Groups[1].Value);
                if (string.IsNullOrWhiteSpace(value))
                {
                    continue;
                }

                if (TryParseAmountValue(value, out var parsed))
                {
                    if (parsed >= bestValue)
                    {
                        bestValue = parsed;
                        bestCandidate = value;
                    }

                    continue;
                }

                bestCandidate ??= value;
            }

            return bestCandidate;
        }

        private static string? NormalizeAmountCandidate(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            var trimmed = value.Trim().TrimEnd('.', ',', ';', ':');
            trimmed = Regex.Replace(trimmed, "\\s+", "");
            return string.IsNullOrWhiteSpace(trimmed) ? null : trimmed;
        }

        private static bool TryParseAmountValue(string value, out decimal amount)
        {
            amount = 0;
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            var normalized = value.Replace(" ", "", StringComparison.Ordinal)
                .Replace("'", "", StringComparison.Ordinal);

            var lastComma = normalized.LastIndexOf(',');
            var lastDot = normalized.LastIndexOf('.');

            if (lastComma > -1 && lastDot > -1)
            {
                if (lastComma > lastDot)
                {
                    normalized = normalized.Replace(".", "", StringComparison.Ordinal).Replace(',', '.');
                }
                else
                {
                    normalized = normalized.Replace(",", "", StringComparison.Ordinal);
                }
            }
            else if (lastComma > -1)
            {
                normalized = ShouldTreatAsDecimal(normalized, lastComma) ? normalized.Replace(',', '.') : normalized.Replace(",", "", StringComparison.Ordinal);
            }
            else if (lastDot > -1 && !ShouldTreatAsDecimal(normalized, lastDot))
            {
                normalized = normalized.Replace(".", "", StringComparison.Ordinal);
            }

            return decimal.TryParse(normalized, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out amount);
        }

        private static bool ShouldTreatAsDecimal(string value, int separatorIndex)
        {
            if (separatorIndex < 0 || separatorIndex >= value.Length - 1)
            {
                return false;
            }

            var decimals = value[(separatorIndex + 1)..];
            return decimals.Length is 1 or 2;
        }

        private DateTime? FindInvoiceDate(IReadOnlyList<string> lines)
        {
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                if (!DateKeywords.Any(keyword => line.Contains(keyword, StringComparison.OrdinalIgnoreCase)))
                {
                    continue;
                }

                var date = ExtractDateFromLine(line);
                if (date.HasValue)
                {
                    return date.Value;
                }
            }

            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                var date = ExtractDateFromLine(line);
                if (date.HasValue)
                {
                    return date.Value;
                }
            }

            return null;
        }

        private DateTime? ExtractDateFromLine(string line)
        {
            var match = DateCandidateRegex.Match(line);
            if (!match.Success)
            {
                return null;
            }

            var candidate = match.Groups["date"].Value;
            var formats = new[] { "dd.MM.yyyy", "d.M.yyyy", "dd/MM/yyyy", "d/M/yyyy", "dd-MM-yyyy", "d-M-yyyy", "yyyy-MM-dd", "yyyy/MM/dd" };

            if (!string.IsNullOrWhiteSpace(_culture))
            {
                try
                {
                    var culture = CultureInfo.GetCultureInfo(_culture);
                    if (DateTime.TryParse(candidate, culture, DateTimeStyles.AssumeLocal, out var parsedFromCulture))
                    {
                        return parsedFromCulture.Date;
                    }
                }
                catch (CultureNotFoundException)
                {
                }
            }

            foreach (var format in formats)
            {
                if (DateTime.TryParseExact(candidate, format, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
                {
                    return parsed.Date;
                }
            }

            if (DateTime.TryParse(candidate, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var fallback))
            {
                return fallback.Date;
            }

            return null;
        }

        private string? FindCurrency(IReadOnlyList<string> lines)
        {
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                var match = Regex.Match(line, @"\b(EUR|USD|GBP|CHF|CAD|AUD|JPY|SEK|NOK|DKK|PLN|CZK|HUF|RON|TRY|RUB|CNY|INR|BRL)\b", RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    return match.Value.ToUpperInvariant();
                }

                if (line.Contains('€'))
                {
                    return "EUR";
                }

                if (line.Contains('$'))
                {
                    return "USD";
                }

                if (line.Contains('£'))
                {
                    return "GBP";
                }
            }

            return null;
        }

        private DebtorInfo FindDebtor(IReadOnlyList<string> lines)
        {
            var metadataBlock = ExtractBlockBeforeMetadata(lines);
            if (metadataBlock.Count > 0)
            {
                var parsedBlock = ParseBlock(metadataBlock);
                if (parsedBlock.Score > 0 && !string.IsNullOrWhiteSpace(parsedBlock.Info.Company))
                {
                    return parsedBlock.Info;
                }
            }

            var cutoff = lines.TakeWhile(line => !Regex.IsMatch(line, "\\b(Rechnung|Invoice)\\b", RegexOptions.IgnoreCase)).ToList();
            if (cutoff.Count == 0)
            {
                cutoff = lines.ToList();
            }

            var blocks = SplitIntoBlocks(cutoff);
            DebtorInfo? best = null;
            var bestScore = int.MinValue;

            foreach (var block in blocks)
            {
                var info = ParseBlock(block);
                if (info.Score > bestScore)
                {
                    bestScore = info.Score;
                    best = info.Info;
                }
            }

            return best ?? new DebtorInfo();
        }

        private static List<string> ExtractBlockBeforeMetadata(IReadOnlyList<string> lines)
        {
            for (var i = 0; i < lines.Count; i++)
            {
                var line = lines[i];
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                if (InvoiceMetadataKeywords.Any(keyword => line.Contains(keyword, StringComparison.OrdinalIgnoreCase)))
                {
                    var block = new List<string>();
                    var index = i - 1;

                    while (index >= 0 && block.Count < 8)
                    {
                        var candidate = lines[index];
                        if (string.IsNullOrWhiteSpace(candidate))
                        {
                            break;
                        }

                        if (block.Count > 0 && SupplierIndicators.Any(indicator => candidate.Contains(indicator, StringComparison.OrdinalIgnoreCase)))
                        {
                            break;
                        }

                        block.Add(candidate);
                        index--;
                    }

                    block.Reverse();
                    return block;
                }
            }

            return new List<string>();
        }

        private static List<List<string>> SplitIntoBlocks(IEnumerable<string> lines)
        {
            var blocks = new List<List<string>>();
            var current = new List<string>();

            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line))
                {
                    if (current.Count > 0)
                    {
                        blocks.Add(current);
                        current = new List<string>();
                    }

                    continue;
                }

                current.Add(line);
            }

            if (current.Count > 0)
            {
                blocks.Add(current);
            }

            return blocks;
        }

        private (DebtorInfo Info, int Score) ParseBlock(List<string> block)
        {
            string? company = null;
            string? street = null;
            string? postal = null;
            string? city = null;
            string? contactName = null;
            string? contactEmail = null;
            string? contactPhone = null;
            var score = 0;

            var segments = block
                .SelectMany(line => line.Split(new[] { '•', '|', '–', '-', ';' }, StringSplitOptions.RemoveEmptyEntries))
                .Select(segment => segment.Trim())
                .Where(segment => !string.IsNullOrWhiteSpace(segment))
                .ToList();

            if (segments.Count == 0)
            {
                return (new DebtorInfo(), 0);
            }

            var bestCompanyScore = int.MinValue;

            for (var index = segments.Count - 1; index >= 0; index--)
            {
                var segment = segments[index];

                if (SupplierIndicators.Any(indicator => segment.Contains(indicator, StringComparison.OrdinalIgnoreCase)))
                {
                    score -= 4;
                    continue;
                }

                if (contactEmail is null)
                {
                    var emailMatch = EmailRegex.Match(segment);
                    if (emailMatch.Success)
                    {
                        contactEmail = emailMatch.Value;
                        score += 3;
                    }
                }

                if (contactPhone is null)
                {
                    var phoneMatch = PhoneRegex.Match(segment);
                    if (phoneMatch.Success && phoneMatch.Value.Count(char.IsDigit) >= 6)
                    {
                        contactPhone = phoneMatch.Value.Trim();
                        score += 2;
                    }
                }

                if (contactName is null)
                {
                    var contactMatch = Regex.Match(segment, @"(?:GF|Geschäftsführer|Managing Director|Director|Contact|Kontakt)[:\s]+(?<value>.+)", RegexOptions.IgnoreCase);
                    if (contactMatch.Success)
                    {
                        contactName = contactMatch.Groups["value"].Value.Trim();
                        score += 3;
                        continue;
                    }

                    if (LooksLikePersonName(segment))
                    {
                        contactName = segment;
                        score += 2;
                        continue;
                    }
                }

                var proximity = segments.Count - index;

                if (postal is null || city is null)
                {
                    var postalMatch = PostalCityRegex.Match(segment);
                    if (postalMatch.Success)
                    {
                        var assigned = false;

                        if (postal is null)
                        {
                            postal = postalMatch.Groups["postal"].Value.Trim();
                            assigned = true;
                        }

                        if (city is null)
                        {
                            city = postalMatch.Groups["city"].Value.Trim();
                            assigned = true;
                        }

                        if (assigned)
                        {
                            score += 4;
                        }

                        continue;
                    }
                }

                if (street is null)
                {
                    var hasDigits = segment.Any(char.IsDigit);
                    var containsStreetWord = StreetIndicators.Any(indicator => segment.Contains(indicator, StringComparison.OrdinalIgnoreCase));
                    if (hasDigits && containsStreetWord)
                    {
                        street = segment;
                        score += 3;
                        continue;
                    }
                }

                var hasLetters = segment.Any(char.IsLetter);
                var hasDigitsInSegment = segment.Any(char.IsDigit);

                if (hasLetters && !hasDigitsInSegment)
                {
                    var candidateScore = 20 - proximity;
                    if (segment.All(static c => !char.IsLetter(c) || char.IsUpper(c)))
                    {
                        candidateScore += 3;
                    }

                    if (LooksLikePersonName(segment))
                    {
                        candidateScore -= 5;
                    }

                    if (candidateScore > bestCompanyScore)
                    {
                        if (company is null)
                        {
                            score += 2;
                        }

                        company = segment;
                        bestCompanyScore = candidateScore;
                    }
                }
            }

            if (company is null)
            {
                company = segments.FirstOrDefault();
            }

            return (new DebtorInfo(company, street, postal, city, contactName, contactEmail, contactPhone), score);
        }

        private string? BuildDescription(IReadOnlyList<string> lines)
        {
            var descriptionCandidates = lines
                .Where(line => line.Contains("description", StringComparison.OrdinalIgnoreCase) || line.Contains("leistungen", StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (descriptionCandidates.Count > 0)
            {
                return descriptionCandidates.First();
            }

            var itemLines = lines
                .Where(line => Regex.IsMatch(line, @"\b\d+\s+x\s+", RegexOptions.IgnoreCase) || line.Split(' ').Length <= 6)
                .Take(2)
                .ToList();

            return itemLines.Count > 0 ? string.Join("; ", itemLines) : null;
        }

        private sealed record DebtorInfo(
            string? Company = null,
            string? Street = null,
            string? PostalCode = null,
            string? City = null,
            string? ContactName = null,
            string? ContactEmail = null,
            string? ContactPhone = null);

        private static bool LooksLikePersonName(string segment)
        {
            var parts = segment
                .Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length is < 2 or > 3)
            {
                return false;
            }

            foreach (var part in parts)
            {
                if (!char.IsLetter(part[0]) || !char.IsUpper(part[0]))
                {
                    return false;
                }

                for (var i = 1; i < part.Length; i++)
                {
                    if (!char.IsLetter(part[i]))
                    {
                        return false;
                    }

                    if (!char.IsLower(part[i]))
                    {
                        return false;
                    }
                }
            }

            return true;
        }
    }
}
