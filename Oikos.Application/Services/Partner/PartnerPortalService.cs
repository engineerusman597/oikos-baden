using Microsoft.EntityFrameworkCore;
using Oikos.Application.Common;
using Oikos.Application.Data;
using Oikos.Application.Services.Partner.Models;
using Oikos.Application.Services.Security;
using Oikos.Common.Constants;
using PartnerEntity = Oikos.Domain.Entities.Partner.Partner;
using UserEntity = Oikos.Domain.Entities.Rbac.User;
using Oikos.Domain.Entities.Rbac;

namespace Oikos.Application.Services.Partner;

public class PartnerPortalService : IPartnerPortalService
{
    private readonly IAppDbContextFactory _dbFactory;
    private readonly IPasswordHasher _passwordHasher;

    public PartnerPortalService(IAppDbContextFactory dbFactory, IPasswordHasher passwordHasher)
    {
        _dbFactory = dbFactory;
        _passwordHasher = passwordHasher;
    }

    public async Task<PartnerPortalDashboardDto?> GetDashboardAsync(int userId)
    {
        await using var context = await _dbFactory.CreateDbContextAsync();

        var partner = await GetPartnerForUserAsync(context, userId);
        if (partner is null) return null;

        var partnerRoleNameDash = RoleNames.Partner.ToRoleName();
        var partnerUserIdsDash = await context.UserRoles
            .AsNoTracking()
            .Where(ur => context.Roles.Any(r => r.Id == ur.RoleId && r.Name == partnerRoleNameDash))
            .Select(ur => ur.UserId)
            .ToListAsync();

        var recommendationsCount = await context.Users
            .AsNoTracking()
            .CountAsync(u => u.PartnerId == partner.Id && !partnerUserIdsDash.Contains(u.Id));

        var subPartnerCount = await context.Partners
            .AsNoTracking()
            .CountAsync(p => p.ParentPartnerId == partner.Id);

        var (commissionPaid, _, openCommission, _, _) = await ComputeCommissionsAsync(context, partner);

        return new PartnerPortalDashboardDto(
            PartnerName: partner.Name,
            ReferralCode: partner.Code,
            RecommendationsCount: recommendationsCount,
            CommissionPaid: commissionPaid,
            OpenCommission: openCommission,
            SubPartnerCount: subPartnerCount);
    }

    public async Task<List<PartnerRecommendationDto>> GetRecommendationsAsync(int userId)
    {
        await using var context = await _dbFactory.CreateDbContextAsync();

        var partner = await GetPartnerForUserAsync(context, userId);
        if (partner is null) return new List<PartnerRecommendationDto>();

        var partnerRoleName = RoleNames.Partner.ToRoleName();
        var partnerUserIds = await context.UserRoles
            .AsNoTracking()
            .Where(ur => context.Roles.Any(r => r.Id == ur.RoleId && r.Name == partnerRoleName))
            .Select(ur => ur.UserId)
            .ToListAsync();

        var users = await context.Users
            .AsNoTracking()
            .Where(u => u.PartnerId == partner.Id && !partnerUserIds.Contains(u.Id))
            .ToListAsync();

        var userIds = users.Select(u => u.Id).ToList();

        var subscriptions = await context.UserSubscriptions
            .AsNoTracking()
            .Include(s => s.SubscriptionPlan)
            .Where(s => userIds.Contains(s.UserId))
            .ToListAsync();

        var subMap = subscriptions
            .GroupBy(s => s.UserId)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(s => s.ActivationDate).First());

        return users.Select(u =>
        {
            subMap.TryGetValue(u.Id, out var sub);
            return new PartnerRecommendationDto(
                UserId: u.Id,
                Name: u.RealName ?? u.Name,
                CustomerNumber: u.CustomerNumber,
                PlanName: sub?.SubscriptionPlan?.Name,
                SubscriptionStatus: sub?.Status ?? "none",
                BillingInterval: sub?.BillingInterval ?? "monthly",
                ExpirationDate: sub?.ExpirationDate);
        }).ToList();
    }

    public async Task<(decimal CommissionPaid, int PaidCount, decimal OpenCommission, int OpenCount, List<PartnerCommissionDto> Statements)> GetCommissionsAsync(int userId)
    {
        await using var context = await _dbFactory.CreateDbContextAsync();

        var partner = await GetPartnerForUserAsync(context, userId);
        if (partner is null)
            return (0m, 0, 0m, 0, new List<PartnerCommissionDto>());

        return await ComputeCommissionsAsync(context, partner);
    }

    public async Task<List<PartnerSubPartnerDto>> GetSubPartnersAsync(int userId)
    {
        await using var context = await _dbFactory.CreateDbContextAsync();

        var partner = await GetPartnerForUserAsync(context, userId);
        if (partner is null) return new List<PartnerSubPartnerDto>();

        var subPartners = await context.Partners
            .AsNoTracking()
            .Where(p => p.ParentPartnerId == partner.Id)
            .OrderBy(p => p.Name)
            .ToListAsync();

        return subPartners.Select(p => new PartnerSubPartnerDto(
            PartnerId: p.Id,
            Name: p.Name,
            Notes: p.Notes,
            ContactEmail: p.ContactEmail,
            Code: p.Code,
            IsActive: p.IsActive,
            CreatedAt: p.CreatedAt)).ToList();
    }

    public async Task<(bool Success, string? Error)> CreateSubPartnerAsync(int parentUserId, CreateSubPartnerRequest request)
    {
        try
        {
            await using var context = await _dbFactory.CreateDbContextAsync();

            var parentPartner = await GetPartnerForUserAsync(context, parentUserId);
            if (parentPartner is null)
                return (false, "Parent partner not found.");

            var email = request.Email.Trim().ToLowerInvariant();

            var userExists = await context.Users.AnyAsync(u =>
                u.Name.ToLower() == email || (u.Email != null && u.Email.ToLower() == email));
            if (userExists)
                return (false, "A user with this email already exists.");

            // Generate unique partner code
            var code = await GenerateUniquePartnerCodeAsync(context);

            // Create partner record
            var partner = new PartnerEntity
            {
                Name = request.Name.Trim(),
                Code = code,
                ContactEmail = email,
                PhoneNumber = string.IsNullOrWhiteSpace(request.PhoneNumber) ? null : request.PhoneNumber.Trim(),
                Notes = string.IsNullOrWhiteSpace(request.Business) ? null : request.Business.Trim(),
                CommissionType = request.CommissionType,
                CommissionRate = request.CommissionRate,
                IsActive = true,
                ParentPartnerId = parentPartner.Id,
                CreatedAt = DateTime.UtcNow
            };
            context.Partners.Add(partner);
            await context.SaveChangesAsync();

            // Create user account linked to this partner
            var customerNumber = await CustomerNumberHelper.GenerateUniqueCustomerNumberAsync(context);
            var user = new UserEntity
            {
                Name = email,
                RealName = request.Name.Trim(),
                Email = email,
                Company = string.IsNullOrWhiteSpace(request.Business) ? null : request.Business.Trim(),
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
            await context.SaveChangesAsync();

            // Assign Partner role
            var partnerRole = await context.Roles.FirstOrDefaultAsync(r => r.Name == RoleNames.Partner.ToRoleName());
            if (partnerRole != null)
            {
                context.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = partnerRole.Id });
                await context.SaveChangesAsync();
            }

            return (true, null);
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    // ── helpers ──────────────────────────────────────────────────────────────

    private static async Task<PartnerEntity?> GetPartnerForUserAsync(IAppDbContext context, int userId)
    {
        var user = await context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user?.PartnerId is null) return null;

        return await context.Partners
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == user.PartnerId.Value);
    }

    private static async Task<(decimal CommissionPaid, int PaidCount, decimal OpenCommission, int OpenCount, List<PartnerCommissionDto> Statements)>
        ComputeCommissionsAsync(IAppDbContext context, PartnerEntity partner)
    {
        var rate = partner.CommissionRate ?? 0m;

        var subscriptions = await context.UserSubscriptions
            .AsNoTracking()
            .Include(s => s.SubscriptionPlan)
            .Include(s => s.User)
            .Where(s => s.User.PartnerId == partner.Id)
            .ToListAsync();

        var statements = new List<PartnerCommissionDto>();
        decimal paid = 0m;
        decimal open = 0m;
        int paidCount = 0;
        int openCount = 0;

        foreach (var sub in subscriptions)
        {
            if (sub.SubscriptionPlan is null) continue;

            var price = sub.BillingInterval == "yearly"
                ? sub.SubscriptionPlan.YearlyPrice
                : sub.SubscriptionPlan.MonthlyPrice;

            var amount = Math.Round(price * rate / 100m, 2);
            var periodEnd = sub.ExpirationDate ?? sub.ActivationDate.AddMonths(
                sub.BillingInterval == "yearly" ? 12 : 1);

            var isActive = sub.Status == "active"
                && (!sub.ExpirationDate.HasValue || sub.ExpirationDate.Value > DateTime.UtcNow);

            if (isActive)
            {
                open += amount;
                openCount++;
                statements.Add(new PartnerCommissionDto(amount, sub.ActivationDate, periodEnd, "Pending"));
            }
            else
            {
                paid += amount;
                paidCount++;
                statements.Add(new PartnerCommissionDto(amount, sub.ActivationDate, periodEnd, "Approved"));
            }
        }

        statements = statements.OrderByDescending(s => s.PeriodStart).ToList();

        return (paid, paidCount, open, openCount, statements);
    }

    private static async Task<string> GenerateUniquePartnerCodeAsync(IAppDbContext context)
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        var rng = new Random();
        for (var attempt = 0; attempt < 20; attempt++)
        {
            var code = "P-" + new string(Enumerable.Range(0, 6).Select(_ => chars[rng.Next(chars.Length)]).ToArray());
            var exists = await context.Partners.AnyAsync(p => p.Code == code);
            if (!exists) return code;
        }
        throw new InvalidOperationException("Unable to generate a unique partner code.");
    }
}
