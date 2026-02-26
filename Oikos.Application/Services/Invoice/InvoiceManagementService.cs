using Microsoft.EntityFrameworkCore;
using Oikos.Application.Data;
using Oikos.Application.Services.Invoice.Models;
using Oikos.Common.Helpers;
using Oikos.Domain.Entities.Invoice;
using Oikos.Domain.Enums;

namespace Oikos.Application.Services.Invoice;

public class InvoiceManagementService : IInvoiceManagementService
{
    private readonly IAppDbContextFactory _dbFactory;

    public InvoiceManagementService(IAppDbContextFactory dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<PagedResult<InvoiceListItemDto>> SearchInvoicesAsync(InvoiceSearchRequest request, string culture)
    {
        using var context = await _dbFactory.CreateDbContextAsync();

        var query = from invoice in context.Invoices.AsNoTracking()
                    join user in context.Users.AsNoTracking() on invoice.UserId equals user.Id
                    join stage in context.InvoiceStages.AsNoTracking() on invoice.StageId equals stage.Id
                    select new { Invoice = invoice, User = user, Stage = stage };

        // Apply search filter
        if (!string.IsNullOrWhiteSpace(request.SearchText))
        {
            var term = request.SearchText.Trim();
            var hasInvoiceId = int.TryParse(term, out var invoiceId);

            query = query.Where(x =>
                x.User.Name.Contains(term) ||
                (x.User.Email != null && x.User.Email.Contains(term)) ||
                (x.User.CustomerNumber != null && x.User.CustomerNumber.Contains(term)) ||
                (x.Invoice.Company != null && x.Invoice.Company.Contains(term)) ||
                (hasInvoiceId && x.Invoice.Id == invoiceId));
        }

        // Apply stage filter
        if (request.StageId.HasValue)
        {
            var stageId = request.StageId.Value;
            query = query.Where(x => x.Invoice.StageId == stageId);
        }

        // Apply primary status filter — multi-value takes precedence over single-value
        if (request.PrimaryStatuses is { Count: > 0 })
        {
            var statuses = request.PrimaryStatuses;
            query = query.Where(x => statuses.Contains(x.Invoice.PrimaryStatus));
        }
        else if (request.PrimaryStatus.HasValue)
        {
            var primaryStatus = request.PrimaryStatus.Value;
            query = query.Where(x => x.Invoice.PrimaryStatus == primaryStatus);
        }

        var totalCount = await query.CountAsync();

        var skip = (request.Page - 1) * request.PageSize;

        var results = await query
            .OrderByDescending(x => x.Invoice.UpdatedAt)
            .ThenByDescending(x => x.Invoice.CreatedAt)
            .Skip(skip)
            .Take(request.PageSize)
            .Select(x => new InvoiceListItemDto(
                x.Invoice.Id,
                0, // Will be set after retrieval
                x.User.Name,
                x.User.Email,
                x.Invoice.Company,
                x.Invoice.Amount,
                x.Invoice.Currency,
                x.Invoice.TicketNumber,
                x.Invoice.StageId,
                LocalizeStageValue(x.Stage.Name, x.Stage.NameDe, culture) ?? x.Stage.Name,
                x.Invoice.PrimaryStatus,
                x.Stage.Color,
                x.Invoice.UpdatedAt,
                x.Invoice.CreatedAt))
            .ToListAsync();

        // Set row numbers
        var items = new List<InvoiceListItemDto>();
        for (var index = 0; index < results.Count; index++)
        {
            var item = results[index];
            items.Add(item with { Number = skip + index + 1 });
        }

        return new PagedResult<InvoiceListItemDto>(items, totalCount, request.Page, request.PageSize);
    }

    public async Task<List<InvoiceStageDto>> GetInvoiceStagesAsync(string culture)
    {
        using var context = await _dbFactory.CreateDbContextAsync();
        
        var stages = await context.InvoiceStages.AsNoTracking()
            .OrderBy(s => s.DisplayOrder)
            .ThenBy(s => s.Id)
            .Select(s => new InvoiceStageDto(
                s.Id,
                LocalizeStageValue(s.Name, s.NameDe, culture) ?? s.Name,
                s.Color,
                s.Icon,
                s.PrimaryStatus))
            .ToListAsync();

        return stages;
    }

    public async Task<bool> ChangeInvoiceStageAsync(int invoiceId, int stageId, int userId, string userName, string? note = null)
    {
        using var context = await _dbFactory.CreateDbContextAsync();

        var entity = await context.Invoices.FirstOrDefaultAsync(x => x.Id == invoiceId);
        if (entity is null)
        {
            return false;
        }

        var stage = await context.InvoiceStages.FirstOrDefaultAsync(x => x.Id == stageId);
        if (stage is null)
        {
            return false;
        }

        entity.StageId = stageId;
        entity.PrimaryStatus = stage.PrimaryStatus;
        entity.UpdatedAt = DateTime.Now;

        context.InvoiceStageHistories.Add(new InvoiceStageHistory
        {
            InvoiceId = entity.Id,
            StageId = stageId,
            ChangedAt = DateTime.Now,
            ChangedByUserId = userId,
            ChangedByUserName = userName,
            Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim()
        });

        await context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> AddInvoiceNoteAsync(int invoiceId, int userId, string userName, string note)
    {
        using var context = await _dbFactory.CreateDbContextAsync();

        var entity = await context.Invoices.FirstOrDefaultAsync(x => x.Id == invoiceId);
        if (entity is null) return false;

        context.InvoiceStageHistories.Add(new InvoiceStageHistory
        {
            InvoiceId = invoiceId,
            StageId = entity.StageId,
            ChangedAt = DateTime.Now,
            ChangedByUserId = userId,
            ChangedByUserName = userName,
            Note = note.Trim()
        });

        await context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteInvoiceAsync(int invoiceId, string storageRoot)
    {
        using var context = await _dbFactory.CreateDbContextAsync();
        
        var entity = await context.Invoices.FirstOrDefaultAsync(x => x.Id == invoiceId);
        if (entity is null)
        {
            return false;
        }

        var filePath = entity.FilePath;
        var powerOfAttorneyPath = entity.PowerOfAttorneyPath;

        var histories = context.InvoiceStageHistories.Where(h => h.InvoiceId == entity.Id);
        context.InvoiceStageHistories.RemoveRange(histories);
        context.Invoices.Remove(entity);
        
        await context.SaveChangesAsync();

        // Delete physical files
        if (!string.IsNullOrWhiteSpace(entity.FilePath))
        {
             var absolutePath = Path.Combine(storageRoot, entity.FilePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
             FileHelper.TryDeleteFile(absolutePath);
        }
        
        if (!string.IsNullOrWhiteSpace(entity.PowerOfAttorneyPath))
        {
             var absolutePath = Path.Combine(storageRoot, entity.PowerOfAttorneyPath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
             FileHelper.TryDeleteFile(absolutePath);
        }

        return true;
    }

    private static string? LocalizeStageValue(string? english, string? german, string culture)
    {
        var twoLetterCode = culture.Length >= 2 ? culture.Substring(0, 2).ToLowerInvariant() : culture.ToLowerInvariant();
        return twoLetterCode switch
        {
            "de" => FirstNonEmpty(german, english),
            _ => FirstNonEmpty(english, german)

        };
    }

    private static string? FirstNonEmpty(params string?[] values)
        => values.FirstOrDefault(v => !string.IsNullOrWhiteSpace(v));



    public async Task<List<InvoiceStageListDto>> GetStageListAsync()
    {
        using var context = await _dbFactory.CreateDbContextAsync();
        
        var stages = await context.InvoiceStages.AsNoTracking()
            .OrderBy(s => s.DisplayOrder)
            .ThenBy(s => s.Id)
            .Select(s => new InvoiceStageListDto(
                s.Id,
                s.Name,
                s.Slug,
                s.Summary ?? string.Empty,
                s.DisplayOrder,
                s.UpdatedAt,
                false,
                false))
            .ToListAsync();

        // Mark first and last
        for (var index = 0; index < stages.Count; index++)
        {
            var stage = stages[index];
            stages[index] = stage with
            {
                IsFirst = index == 0,
                IsLast = index == stages.Count - 1
            };
        }

        return stages;
    }

    public async Task<bool> DeleteStageAsync(int stageId)
    {
        using var context = await _dbFactory.CreateDbContextAsync();
        
        var entity = await context.InvoiceStages.FirstOrDefaultAsync(s => s.Id == stageId);
        if (entity is null)
        {
            return false;
        }

        // Check if stage is in use
        var hasInvoices = await context.Invoices.AnyAsync(i => i.StageId == entity.Id);
        if (hasInvoices)
        {
            return false;
        }

        context.InvoiceStages.Remove(entity);
        await context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> MoveStageAsync(int stageId, int offset)
    {
        using var context = await _dbFactory.CreateDbContextAsync();
        
        var stages = await context.InvoiceStages
            .OrderBy(s => s.DisplayOrder)
            .ThenBy(s => s.Id)
            .ToListAsync();

        var currentIndex = stages.FindIndex(s => s.Id == stageId);
        if (currentIndex < 0)
        {
            return false;
        }

        var targetIndex = currentIndex + offset;
        if (targetIndex < 0 || targetIndex >= stages.Count)
        {
            return false;
        }

        var currentStage = stages[currentIndex];
        var targetStage = stages[targetIndex];

        // Swap display orders
        (currentStage.DisplayOrder, targetStage.DisplayOrder) = (targetStage.DisplayOrder, currentStage.DisplayOrder);
        currentStage.UpdatedAt = DateTime.Now;
        targetStage.UpdatedAt = DateTime.Now;

        await context.SaveChangesAsync();
        return true;
    }

    public async Task<InvoiceDetailDto?> GetInvoiceDetailAsync(int invoiceId, string culture)
    {
        using var context = await _dbFactory.CreateDbContextAsync();

        var invoice = await (from inv in context.Invoices
                            join user in context.Users on inv.UserId equals user.Id
                            where inv.Id == invoiceId
                            select new
                            {
                                Invoice = inv,
                                User = user,
                                Stage = inv.Stage
                            }).FirstOrDefaultAsync();

        if (invoice == null)
        {
            return null;
        }

        // Get history
        var history = await context.InvoiceStageHistories
            .Where(h => h.InvoiceId == invoiceId)
            .OrderByDescending(h => h.ChangedAt)
            .Select(h => new InvoiceHistoryDto(
                h.Stage.Slug,
                LocalizeStageValue(h.Stage.Name, h.Stage.NameDe, culture) ?? h.Stage.Name,
                h.Stage.Color,
                string.IsNullOrWhiteSpace(h.Stage.Icon) ? "flag" : h.Stage.Icon!,
                h.ChangedAt,
                h.ChangedByUserName,
                h.ChangedByUserId,
                h.Note))
            .ToListAsync();

        var stage = invoice.Stage;
        var filePath = invoice.Invoice.FilePath;
        var powerOfAttorneyPath = invoice.Invoice.PowerOfAttorneyPath;

        return new InvoiceDetailDto(
            Id: invoice.Invoice.Id,
            Company: invoice.Invoice.Company,
            Amount: invoice.Invoice.Amount,
            Currency: invoice.Invoice.Currency,
            InvoiceDate: invoice.Invoice.InvoiceDate,
            TicketNumber: invoice.Invoice.TicketNumber,
            StageId: invoice.Invoice.StageId,
            PrimaryStatus: invoice.Invoice.PrimaryStatus,
            StageSlug: stage.Slug,
            StageName: LocalizeStageValue(stage.Name, stage.NameDe, culture) ?? stage.Name,
            StageSummary: LocalizeStageValue(stage.Summary, stage.SummaryDe, culture),
            StageDescription: LocalizeStageValue(stage.Description, stage.DescriptionDe, culture),
            StageNextSteps: LocalizeStageValue(stage.NextSteps, stage.NextStepsDe, culture),
            StageColor: stage.Color,
            StageIcon: string.IsNullOrWhiteSpace(stage.Icon) ? "flag" : stage.Icon!,
            UserName: invoice.User.Name,
            UserEmail: invoice.User.Email,
            CustomerNumber: invoice.User.CustomerNumber,
            FilePath: filePath,
            FileName: filePath == null ? null : Path.GetFileName(filePath),
            PowerOfAttorneyPath: powerOfAttorneyPath,
            PowerOfAttorneyFileName: powerOfAttorneyPath == null ? null : Path.GetFileName(powerOfAttorneyPath),
            CreatedAt: invoice.Invoice.CreatedAt,
            UpdatedAt: invoice.Invoice.UpdatedAt,
            History: history);
    }

    public async Task<MyInvoicesDto> GetMyInvoicesAsync(int userId, string culture)
    {
        using var context = await _dbFactory.CreateDbContextAsync();

        // Load user invoices
        var invoices = await context.Invoices
            .Where(i => i.UserId == userId)
            .OrderByDescending(i => i.UpdatedAt)
            .ThenByDescending(i => i.CreatedAt)
            .Select(i => new MyInvoiceItemDto(
                i.Id,
                i.TicketNumber,
                i.Company,
                i.Amount,
                i.Currency,
                i.InvoiceDate,
                i.StageId,
                i.PrimaryStatus,
                i.UpdatedAt,
                i.CreatedAt))
            .ToListAsync();

        // Group invoices by stage
        var invoicesByStage = invoices.GroupBy(i => i.StageId)
            .ToDictionary(g => g.Key, g => g.Count());

        // Load stages with counts
        var stages = await context.InvoiceStages.AsNoTracking()
            .OrderBy(s => s.DisplayOrder)
            .ThenBy(s => s.Id)
            .Select(s => new InvoiceStageWithCountDto(
                s.Id,
                s.Slug,
                LocalizeStageValue(s.Name, s.NameDe, culture) ?? s.Name,
                LocalizeStageValue(s.Summary, s.SummaryDe, culture),
                LocalizeStageValue(s.Description, s.DescriptionDe, culture),
                LocalizeStageValue(s.NextSteps, s.NextStepsDe, culture),
                string.IsNullOrWhiteSpace(s.Icon) ? "flag" : s.Icon!,
                s.Color,
                s.PrimaryStatus,
                0)) // Will be updated below
            .ToListAsync();

        // Update counts
        var stagesWithCounts = stages.Select(s => s with
        {
            Count = invoicesByStage.TryGetValue(s.Id, out var count) ? count : 0
        }).ToList();

        return new MyInvoicesDto(invoices, stagesWithCounts);
    }

    public async Task<InvoiceStageEditDto?> GetStageForEditAsync(int stageId)
    {
        using var context = await _dbFactory.CreateDbContextAsync();
        
        var stage = await context.InvoiceStages.FirstOrDefaultAsync(s => s.Id == stageId);
        if (stage == null)
        {
            return null;
        }

        return new InvoiceStageEditDto(
            Id: stage.Id,
            Name: stage.Name,
            NameDe: stage.NameDe,
            Slug: stage.Slug,
            Summary: stage.Summary,
            SummaryDe: stage.SummaryDe,
            Description: stage.Description,
            DescriptionDe: stage.DescriptionDe,
            NextSteps: stage.NextSteps,
            NextStepsDe: stage.NextStepsDe,
            Icon: stage.Icon,
            Color: stage.Color,
            PrimaryStatus: stage.PrimaryStatus);
    }

    public async Task<SaveStageResult> SaveStageAsync(InvoiceStageEditDto dto)
    {
        using var context = await _dbFactory.CreateDbContextAsync();

        // Check if slug already exists
        var exists = await context.InvoiceStages.AnyAsync(s => s.Slug == dto.Slug && s.Id != dto.Id);
        if (exists)
        {
            return new SaveStageResult(false, "StageManagerSlugExists", null);
        }

        Domain.Entities.Invoice.InvoiceStage entity;
        
        if (dto.Id.HasValue)
        {
            // Update existing
            entity = await context.InvoiceStages.FirstOrDefaultAsync(s => s.Id == dto.Id.Value);
            if (entity == null)
            {
                return new SaveStageResult(false, "StageManagerNotFound", null);
            }
        }
        else
        {
            // Create new
            var maxOrder = await context.InvoiceStages.MaxAsync(s => (int?)s.DisplayOrder) ?? 0;
            entity = new Domain.Entities.Invoice.InvoiceStage
            {
                DisplayOrder = maxOrder + 1,
                CreatedAt = DateTime.Now
            };
            context.InvoiceStages.Add(entity);
        }

        // Update properties
        entity.Name = dto.Name.Trim();
        entity.NameDe = string.IsNullOrWhiteSpace(dto.NameDe) ? dto.Name : dto.NameDe.Trim();
        entity.Slug = dto.Slug.Trim();
        entity.Summary = string.IsNullOrWhiteSpace(dto.Summary) ? null : dto.Summary.Trim();
        entity.SummaryDe = string.IsNullOrWhiteSpace(dto.SummaryDe) ? null : dto.SummaryDe.Trim();
        entity.Description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim();
        entity.DescriptionDe = string.IsNullOrWhiteSpace(dto.DescriptionDe) ? null : dto.DescriptionDe.Trim();
        entity.NextSteps = string.IsNullOrWhiteSpace(dto.NextSteps) ? null : dto.NextSteps.Trim();
        entity.NextStepsDe = string.IsNullOrWhiteSpace(dto.NextStepsDe) ? null : dto.NextStepsDe.Trim();
        entity.Icon = string.IsNullOrWhiteSpace(dto.Icon) ? "flag" : dto.Icon.Trim();
        entity.Color = string.IsNullOrWhiteSpace(dto.Color) ? null : dto.Color;
        entity.PrimaryStatus = dto.PrimaryStatus;
        entity.UpdatedAt = DateTime.Now;

        await context.SaveChangesAsync();
        
        return new SaveStageResult(true, null, entity.Id);
    }
}
