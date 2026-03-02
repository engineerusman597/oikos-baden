using Microsoft.EntityFrameworkCore;
using Oikos.Application.Common;
using Oikos.Application.Data;
using Oikos.Application.Services.Partner.Models;
using Oikos.Application.Services.Security;
using Oikos.Common.Constants;
using Oikos.Domain.Entities.Rbac;
using PartnerEntity = Oikos.Domain.Entities.Partner.Partner;
using UserEntity = Oikos.Domain.Entities.Rbac.User;

namespace Oikos.Application.Services.Partner;

public class PartnerService : IPartnerService
{
    private readonly IAppDbContextFactory _dbFactory;
    private readonly IPasswordHasher _passwordHasher;

    public PartnerService(IAppDbContextFactory dbFactory, IPasswordHasher passwordHasher)
    {
        _dbFactory = dbFactory;
        _passwordHasher = passwordHasher;
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

        if (string.IsNullOrWhiteSpace(request.Name))
            throw new ArgumentException("Partner name is required.", nameof(request));
        var code = string.IsNullOrWhiteSpace(request.Code)
            ? await GenerateUniqueCodeAsync(context, cancellationToken)
            : NormalizeCode(request.Code)!;
        await EnsureCodeIsUniqueAsync(context, code, null, cancellationToken);

        var partner = new PartnerEntity
        {
            Name = request.Name.Trim(),
            PartnerType = string.IsNullOrWhiteSpace(request.PartnerType) ? null : request.PartnerType.Trim(),
            BusinessName = string.IsNullOrWhiteSpace(request.BusinessName) ? null : request.BusinessName.Trim(),
            Code = code,
            ContactEmail = string.IsNullOrWhiteSpace(request.ContactEmail) ? null : request.ContactEmail.Trim(),
            PhoneNumber = string.IsNullOrWhiteSpace(request.PhoneNumber) ? null : request.PhoneNumber.Trim(),
            ContactPerson = string.IsNullOrWhiteSpace(request.ContactPerson) ? null : request.ContactPerson.Trim(),
            Address = string.IsNullOrWhiteSpace(request.Address) ? null : request.Address.Trim(),
            Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim(),
            CommissionType = string.IsNullOrWhiteSpace(request.CommissionType) ? null : request.CommissionType.Trim(),
            CommissionRate = request.CommissionRate,
            CommissionPeriodMonths = request.CommissionPeriodMonths,
            IsActive = request.IsActive,
            CreatedAt = DateTime.UtcNow
        };

        context.Partners.Add(partner);
        await context.SaveChangesAsync(cancellationToken);

        // If a password was provided, create the partner user account and assign Partner role
        if (!string.IsNullOrWhiteSpace(request.Password) && !string.IsNullOrWhiteSpace(request.ContactEmail))
        {
            var email = request.ContactEmail.Trim().ToLowerInvariant();
            var customerNumber = await CustomerNumberHelper.GenerateUniqueCustomerNumberAsync(context);
            var user = new UserEntity
            {
                Name = email,
                RealName = request.Name.Trim(),
                Email = email,
                PhoneNumber = string.IsNullOrWhiteSpace(request.PhoneNumber) ? null : request.PhoneNumber.Trim(),
                PasswordHash = _passwordHasher.HashPassword(request.Password),
                IsEnabled = true,
                IsDeleted = false,
                IsSpecial = false,
                CustomerNumber = customerNumber,
                PartnerId = partner.Id,
                AcceptedPrivacyPolicy = false
            };
            context.Users.Add(user);
            await context.SaveChangesAsync(cancellationToken);

            var partnerRole = await context.Roles.FirstOrDefaultAsync(r => r.Name == RoleNames.Partner.ToRoleName(), cancellationToken);
            if (partnerRole != null)
            {
                context.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = partnerRole.Id });
                await context.SaveChangesAsync(cancellationToken);
            }
        }

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

        if (string.IsNullOrWhiteSpace(request.Name))
            throw new ArgumentException("Partner name is required.", nameof(request));
        var code = string.IsNullOrWhiteSpace(request.Code) ? partner.Code : NormalizeCode(request.Code)!;
        await EnsureCodeIsUniqueAsync(context, code, partnerId, cancellationToken);

        partner.Name = request.Name.Trim();
        partner.PartnerType = string.IsNullOrWhiteSpace(request.PartnerType) ? null : request.PartnerType.Trim();
        partner.BusinessName = string.IsNullOrWhiteSpace(request.BusinessName) ? null : request.BusinessName.Trim();
        partner.Code = code;
        partner.ContactEmail = string.IsNullOrWhiteSpace(request.ContactEmail) ? null : request.ContactEmail.Trim();
        partner.PhoneNumber = string.IsNullOrWhiteSpace(request.PhoneNumber) ? null : request.PhoneNumber.Trim();
        partner.ContactPerson = string.IsNullOrWhiteSpace(request.ContactPerson) ? null : request.ContactPerson.Trim();
        partner.Address = string.IsNullOrWhiteSpace(request.Address) ? null : request.Address.Trim();
        partner.Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim();
        partner.CommissionType = string.IsNullOrWhiteSpace(request.CommissionType) ? null : request.CommissionType.Trim();
        partner.CommissionRate = request.CommissionRate;
        partner.CommissionPeriodMonths = request.CommissionPeriodMonths;
        partner.IsActive = request.IsActive;

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

    private static string? NormalizeCode(string? code) =>
        string.IsNullOrWhiteSpace(code) ? null : code.Trim().ToUpperInvariant();

    private static async Task<string> GenerateUniqueCodeAsync(IAppDbContext context, CancellationToken ct)
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        var rng = new Random();
        for (var i = 0; i < 20; i++)
        {
            var c = "P-" + new string(Enumerable.Range(0, 6).Select(_ => chars[rng.Next(chars.Length)]).ToArray());
            if (!await context.Partners.AnyAsync(p => p.Code == c, ct)) return c;
        }
        throw new InvalidOperationException("Unable to generate a unique partner code.");
    }

    private static PartnerDetail ToDetail(PartnerEntity partner, IReadOnlyDictionary<int, int>? counts = null)
    {
        return new PartnerDetail
        {
            Id = partner.Id,
            Name = partner.Name,
            PartnerType = partner.PartnerType,
            BusinessName = partner.BusinessName,
            Code = partner.Code,
            ContactEmail = partner.ContactEmail,
            PhoneNumber = partner.PhoneNumber,
            ContactPerson = partner.ContactPerson,
            Address = partner.Address,
            Notes = partner.Notes,
            CommissionType = partner.CommissionType,
            CommissionRate = partner.CommissionRate,
            CommissionPeriodMonths = partner.CommissionPeriodMonths,
            IsActive = partner.IsActive,
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
