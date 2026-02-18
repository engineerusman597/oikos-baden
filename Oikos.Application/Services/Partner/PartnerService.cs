using Microsoft.EntityFrameworkCore;
using Oikos.Application.Data;
using Oikos.Application.Services.Partner.Models;
using PartnerEntity = Oikos.Domain.Entities.Partner.Partner;

namespace Oikos.Application.Services.Partner;

public class PartnerService : IPartnerService
{
    private readonly IAppDbContextFactory _dbFactory;

    public PartnerService(IAppDbContextFactory dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<IReadOnlyList<PartnerDetail>> GetPartnersAsync(CancellationToken cancellationToken = default)
    {
        await using var context = await _dbFactory.CreateDbContextAsync(cancellationToken);

        var userCounts = await context.Users
            .Where(u => u.PartnerId != null)
            .GroupBy(u => u.PartnerId!.Value)
            .Select(g => new { PartnerId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(k => k.PartnerId, v => v.Count, cancellationToken);

        var partners = await context.Partners
            .AsNoTracking()
            .OrderBy(p => p.Name)
            .ToListAsync(cancellationToken);

        return partners
            .Select(p => ToDetail(p, userCounts))
            .ToList();
    }

    public async Task<PartnerDetail?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        await using var context = await _dbFactory.CreateDbContextAsync(cancellationToken);

        var normalizedCode = NormalizeCode(code);
        if (string.IsNullOrWhiteSpace(normalizedCode))
        {
            return null;
        }

        var partner = await context.Partners
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Code == normalizedCode, cancellationToken);

        return partner == null ? null : ToDetail(partner);
    }

    public async Task<PartnerDetail> CreateAsync(PartnerRequest request, CancellationToken cancellationToken = default)
    {
        await using var context = await _dbFactory.CreateDbContextAsync(cancellationToken);

        var normalizedRequest = NormalizeRequest(request);
        await EnsureCodeIsUniqueAsync(context, normalizedRequest.Code, null, cancellationToken);

        var partner = new PartnerEntity
        {
            Name = normalizedRequest.Name,
            Code = normalizedRequest.Code,
            ContactEmail = normalizedRequest.ContactEmail,
            Notes = normalizedRequest.Notes,
            CreatedAt = DateTime.UtcNow
        };

        context.Partners.Add(partner);
        await context.SaveChangesAsync(cancellationToken);

        return ToDetail(partner);
    }

    public async Task<PartnerDetail> UpdateAsync(int partnerId, PartnerRequest request, CancellationToken cancellationToken = default)
    {
        await using var context = await _dbFactory.CreateDbContextAsync(cancellationToken);

        var partner = await context.Partners.FirstOrDefaultAsync(p => p.Id == partnerId, cancellationToken);
        if (partner == null)
        {
            throw new InvalidOperationException("Partner not found.");
        }

        var normalizedRequest = NormalizeRequest(request);
        await EnsureCodeIsUniqueAsync(context, normalizedRequest.Code, partnerId, cancellationToken);

        partner.Name = normalizedRequest.Name;
        partner.Code = normalizedRequest.Code;
        partner.ContactEmail = normalizedRequest.ContactEmail;
        partner.Notes = normalizedRequest.Notes;

        await context.SaveChangesAsync(cancellationToken);

        return ToDetail(partner);
    }

    public async Task DeleteAsync(int partnerId, CancellationToken cancellationToken = default)
    {
        await using var context = await _dbFactory.CreateDbContextAsync(cancellationToken);

        var hasLinkedUsers = await context.Users.AnyAsync(u => u.PartnerId == partnerId, cancellationToken);
        if (hasLinkedUsers)
        {
            throw new InvalidOperationException("Cannot delete a partner while customers are linked to it.");
        }

        var partner = await context.Partners.FirstOrDefaultAsync(p => p.Id == partnerId, cancellationToken);
        if (partner == null)
        {
            return;
        }

        context.Partners.Remove(partner);
        await context.SaveChangesAsync(cancellationToken);
    }

    private static PartnerRequest NormalizeRequest(PartnerRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new ArgumentException("Partner name is required.", nameof(request));
        }

        if (string.IsNullOrWhiteSpace(request.Code))
        {
            throw new ArgumentException("Partner code is required.", nameof(request));
        }

        return new PartnerRequest
        {
            Name = request.Name.Trim(),
            Code = NormalizeCode(request.Code)!,
            ContactEmail = string.IsNullOrWhiteSpace(request.ContactEmail) ? null : request.ContactEmail.Trim(),
            Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim()
        };
    }

    private static string? NormalizeCode(string? code)
    {
        return string.IsNullOrWhiteSpace(code)
            ? null
            : code.Trim().ToUpperInvariant();
    }

    private static PartnerDetail ToDetail(PartnerEntity partner, IReadOnlyDictionary<int, int>? counts = null)
    {
        return new PartnerDetail
        {
            Id = partner.Id,
            Name = partner.Name,
            Code = partner.Code,
            ContactEmail = partner.ContactEmail,
            Notes = partner.Notes,
            CreatedAt = partner.CreatedAt,
            CustomerCount = counts != null && counts.TryGetValue(partner.Id, out var count) ? count : 0
        };
    }

    private static async Task EnsureCodeIsUniqueAsync(
        IAppDbContext context,
        string code,
        int? partnerId,
        CancellationToken cancellationToken)
    {
        var exists = await context.Partners
            .AnyAsync(p => p.Code == code && (!partnerId.HasValue || p.Id != partnerId.Value), cancellationToken);

        if (exists)
        {
            throw new InvalidOperationException("A partner with the same code already exists.");
        }
    }
}
