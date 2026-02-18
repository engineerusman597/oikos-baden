using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Oikos.Application.Common.Storage;
using Oikos.Application.Data;
using Oikos.Application.Services.Email;
using Oikos.Common.Helpers;
using Oikos.Application.Services.Invoice.Models;
using Oikos.Application.Services.Subscription;
using Oikos.Application.Services.Email.Templates;

namespace Oikos.Application.Services.Invoice;

public class InvoiceSubmissionService : IInvoiceSubmissionService
{
    private readonly IAppDbContextFactory _dbFactory;
    private readonly IInvoiceExtractionService _extractionService;
    private readonly ISubscriptionPlanService _subscriptionService;
    private readonly IPowerOfAttorneyPdfGenerator _pdfGenerator;
    private readonly IEmailSender _emailSender;
    private readonly IWebHostEnvironment _env;

    public InvoiceSubmissionService(
        IAppDbContextFactory dbFactory,
        IInvoiceExtractionService extractionService,
        ISubscriptionPlanService subscriptionService,
        IPowerOfAttorneyPdfGenerator pdfGenerator,
        IEmailSender emailSender,
        IWebHostEnvironment env)
    {
        _dbFactory = dbFactory;
        _extractionService = extractionService;
        _subscriptionService = subscriptionService;
        _pdfGenerator = pdfGenerator;
        _emailSender = emailSender;
        _env = env;
    }

    public async Task<FileUploadResult> ProcessFileUploadAsync(FileUploadRequest request, CancellationToken cancellationToken = default)
    {
        var storageRoot = GetStorageRoot();
        var tempDirectoryRelative = UserStoragePath.GetRelativePath(request.UserId, "temp");
        var tempDirectory = Path.Combine(storageRoot, tempDirectoryRelative);
        Directory.CreateDirectory(tempDirectory);
        
        var tempFilePath = Path.Combine(tempDirectory, $"{Guid.NewGuid()}.pdf");
        var webPath = GetWebRelativePath(tempFilePath, storageRoot);

        try
        {
            // Save file to temp location
            await using (var fileStream = new FileStream(tempFilePath, FileMode.Create, FileAccess.Write, FileShare.Read, 81920, useAsync: true))
            {
                await request.FileStream.CopyToAsync(fileStream, cancellationToken);
            }

            // Extract invoice data
            var pdfBytes = await File.ReadAllBytesAsync(tempFilePath, cancellationToken);
            var extractionRequest = new InvoiceAiExtractionRequest(
                RawText: string.Empty,
                Culture: "de",
                FileName: request.FileName,
                PdfData: pdfBytes);
            var extraction = await _extractionService.ExtractAsync(extractionRequest, cancellationToken);

            var draft = new InvoiceDraftDto(
                Id: Guid.NewGuid(),
                TempFilePath: tempFilePath,
                WebPath: webPath,
                FileName: request.FileName,
                FileSize: request.FileSize,
                Details: new InvoiceDetailsDto(
                    InvoiceNumber: extraction.InvoiceNumber,
                    Amount: extraction.Amount,
                    Currency: extraction.Currency,
                    InvoiceDate: extraction.InvoiceDate,
                    Description: extraction.Description));

            return new FileUploadResult(true, draft, extraction, null);
        }
        catch
        {
            FileHelper.TryDeleteFile(tempFilePath);
            return new FileUploadResult(false, null, null, "UploadError");
        }
    }

    public async Task<InvoiceSubmissionResult> SubmitInvoicesAsync(InvoiceSubmissionRequest request, CancellationToken cancellationToken = default)
    {
        using var context = await _dbFactory.CreateDbContextAsync(cancellationToken);
        
        // Check subscription
        var claimCheck = await _subscriptionService.CheckClaimSubmissionAsync(request.UserId, request.Drafts.Count);
        
        if (!claimCheck.HasActiveSubscription)
        {
            return new InvoiceSubmissionResult(false, new List<int>(), "SubscriptionRequiredMessage");
        }

        if (claimCheck.IsExpired)
        {
            return new InvoiceSubmissionResult(false, new List<int>(), "WizardSubscriptionExpired");
        }

        if (!claimCheck.IsAllowed)
        {
            return new InvoiceSubmissionResult(false, new List<int>(), "WizardMonthlyClaimLimitExceeded");
        }

        // Get initial stage
        var initialStage = await context.InvoiceStages
            .OrderBy(s => s.DisplayOrder)
            .ThenBy(s => s.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (initialStage == null)
        {
            return new InvoiceSubmissionResult(false, new List<int>(), "WizardMissingStageError");
        }

        var createdIds = new List<int>();
        var storageRoot = GetStorageRoot();

        foreach (var draft in request.Drafts)
        {
            // Move file to permanent storage
            var fileName = $"{Guid.NewGuid()}.pdf";
            var relativeDirectory = UserStoragePath.GetRelativePath(request.UserId, "invoices");
            var absoluteDirectory = Path.Combine(storageRoot, relativeDirectory);
            Directory.CreateDirectory(absoluteDirectory);
            var absoluteFilePath = Path.Combine(absoluteDirectory, fileName);

            if (File.Exists(draft.TempFilePath))
            {
                File.Copy(draft.TempFilePath, absoluteFilePath, true);
            }

            // Generate Power of Attorney PDF
            var poaFileName = $"poa_{fileName}";
            var poaFilePath = Path.Combine(absoluteDirectory, poaFileName);
            
            try
            {
                // Get template path from wwwroot
                var templatePath = Path.Combine(storageRoot, "Vollmacht.pdf");
                
                if (!File.Exists(templatePath))
                {
                    Console.WriteLine($"ERROR: Vollmacht template not found at {templatePath}");
                    // Continue without POA if template missing
                }
                else
                {
                    var poaDetails = new PowerOfAttorneyPdfDetails(
                        CreditorName: request.PowerOfAttorney.Name,
                        CreditorStreet: request.PowerOfAttorney.Street,
                        CreditorPostalCode: request.PowerOfAttorney.PostalCode,
                        CreditorCity: request.PowerOfAttorney.City,
                        DebtorCompany: request.Debtor.CompanyName ?? string.Empty,
                        DebtorStreet: request.Debtor.Street ?? string.Empty,
                        DebtorPostalCode: request.Debtor.PostalCode ?? string.Empty,
                        DebtorCity: request.Debtor.City ?? string.Empty,
                        InvoiceNumber: draft.Details.InvoiceNumber ?? string.Empty,
                        InvoiceDate: draft.Details.InvoiceDate ?? DateTime.Now,
                        InvoiceAmount: draft.Details.Amount ?? string.Empty,
                        InvoiceCurrency: draft.Details.Currency ?? "EUR",
                        Signature: request.PowerOfAttorney.Signature,
                        SignatureDate: request.PowerOfAttorney.SignedAt);

                    // Pass template path, not output path
                    var poaBytes = _pdfGenerator.Generate(poaDetails, templatePath);
                    
                    if (poaBytes.Length > 0)
                    {
                        await File.WriteAllBytesAsync(poaFilePath, poaBytes, cancellationToken);
                        
                        // Verify file was written
                        if (!File.Exists(poaFilePath))
                        {
                            Console.WriteLine($"WARNING: Vollmacht file was not created at {poaFilePath}");
                        }
                        else
                        {
                            var fileInfo = new FileInfo(poaFilePath);
                            Console.WriteLine($"SUCCESS: Vollmacht file created at {poaFilePath} ({fileInfo.Length} bytes)");
                        }
                    }
                    else
                    {
                        Console.WriteLine($"WARNING: PDF generator returned empty byte array for {poaFilePath}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR generating Vollmacht PDF for invoice {draft.Details.InvoiceNumber}: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                // Continue with invoice submission even if POA generation fails
            }

            // Create invoice entity
            var invoice = new Domain.Entities.Invoice.Invoice
            {
                UserId = request.UserId,
                FilePath = Path.Combine(relativeDirectory, fileName).Replace('\\', '/'),
                PowerOfAttorneyPath = Path.Combine(relativeDirectory, poaFileName).Replace('\\', '/'),
                Company = request.Debtor.CompanyName,
                Amount = draft.Details.Amount,
                InvoiceDate = draft.Details.InvoiceDate,
                Currency = draft.Details.Currency,
                Description = draft.Details.Description,
                StageId = initialStage.Id,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            context.Invoices.Add(invoice);
            context.InvoiceStageHistories.Add(new Domain.Entities.Invoice.InvoiceStageHistory
            {
                Invoice = invoice,
                StageId = initialStage.Id,
                ChangedAt = DateTime.Now,
                ChangedByUserId = request.UserId,
                ChangedByUserName = request.UserName
            });

            await context.SaveChangesAsync();
            createdIds.Add(invoice.Id);

            // Delete temp file
            FileHelper.TryDeleteFile(draft.TempFilePath);

            // Send email notification
            if (!string.IsNullOrWhiteSpace(request.UserEmail))
            {
                try
                {
                    var emailBody = InvoiceSubmittedEmailTemplate.Build(
                        request.UserName,
                        invoice.TicketNumber ?? "N/A",
                        request.Debtor.CompanyName ?? "N/A");

                    await _emailSender.SendEmailAsync(request.UserEmail, InvoiceSubmittedEmailTemplate.Subject, emailBody);
                }
                catch
                {
                    // Email failure shouldn't block submission
                }
            }
        }

        return new InvoiceSubmissionResult(true, createdIds, null);
    }



    private string GetStorageRoot()
    {
        if (!string.IsNullOrWhiteSpace(_env.WebRootPath))
        {
            return _env.WebRootPath;
        }

        if (!string.IsNullOrWhiteSpace(_env.ContentRootPath))
        {
            return Path.Combine(_env.ContentRootPath, "wwwroot");
        }

        return Path.Combine(AppContext.BaseDirectory, "wwwroot");
    }

    private string GetWebRelativePath(string absolutePath, string storageRoot)
    {
        if (string.IsNullOrWhiteSpace(absolutePath) || string.IsNullOrWhiteSpace(storageRoot))
        {
            return string.Empty;
        }

        try
        {
            var relative = Path.GetRelativePath(storageRoot, absolutePath).Replace('\\', '/');
            return "/" + relative.TrimStart('/');
        }
        catch
        {
            return string.Empty;
        }
    }
}
