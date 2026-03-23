using Microsoft.EntityFrameworkCore;
using Oikos.Application.Common;
using Oikos.Application.Data;
using Oikos.Application.Services.Security;
using Oikos.Application.Services.Subscription;
using Oikos.Application.Services.TaxOffice.Models;
using Oikos.Common.Constants;
using Oikos.Domain.Entities.Rbac;
using TaxOfficeEntity = Oikos.Domain.Entities.TaxOffice.TaxOffice;
using UserEntity = Oikos.Domain.Entities.Rbac.User;

namespace Oikos.Application.Services.TaxOffice;

public class TaxOfficeService : ITaxOfficeService
{
    private readonly IAppDbContextFactory _dbFactory;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ISubscriptionPlanService _subscriptionPlanService;

    public TaxOfficeService(IAppDbContextFactory dbFactory, IPasswordHasher passwordHasher, ISubscriptionPlanService subscriptionPlanService)
    {
        _dbFactory = dbFactory;
        _passwordHasher = passwordHasher;
        _subscriptionPlanService = subscriptionPlanService;
    }

    public async Task<IReadOnlyList<TaxOfficeDetail>> GetTaxOfficesAsync(CancellationToken cancellationToken = default)
    {
        await using var context = await _dbFactory.CreateDbContextAsync(cancellationToken);

        var offices = await context.TaxOffices
            .AsNoTracking()
            .OrderBy(t => t.Name)
            .ToListAsync(cancellationToken);

        return offices.Select(ToDetail).ToList();
    }

    public async Task<TaxOfficeDetail?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var context = await _dbFactory.CreateDbContextAsync(cancellationToken);
        var entity = await context.TaxOffices.AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
        return entity is null ? null : ToDetail(entity);
    }

    public async Task<TaxOfficeDetail> CreateAsync(TaxOfficeRequest request, CancellationToken cancellationToken = default)
    {
        await using var context = await _dbFactory.CreateDbContextAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(request.Name))
            throw new ArgumentException("Tax office name is required.", nameof(request));

        if (!string.IsNullOrWhiteSpace(request.Email))
        {
            var normalizedEmail = request.Email.Trim().ToLowerInvariant();
            var emailExists = await context.TaxOffices
                .AnyAsync(t => t.Email != null && t.Email.ToLower() == normalizedEmail, cancellationToken);
            if (emailExists)
                throw new InvalidOperationException("A tax office with this email already exists.");
        }

        var code = await GenerateUniqueCodeAsync(context, cancellationToken);

        var entity = new TaxOfficeEntity
        {
            Name = request.Name.Trim(),
            BusinessName = string.IsNullOrWhiteSpace(request.BusinessName) ? null : request.BusinessName.Trim(),
            Email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim(),
            PhoneNumber = string.IsNullOrWhiteSpace(request.PhoneNumber) ? null : request.PhoneNumber.Trim(),
            ContactPerson = string.IsNullOrWhiteSpace(request.ContactPerson) ? null : request.ContactPerson.Trim(),
            Address = string.IsNullOrWhiteSpace(request.Address) ? null : request.Address.Trim(),
            PostalCode = string.IsNullOrWhiteSpace(request.PostalCode) ? null : request.PostalCode.Trim(),
            City = string.IsNullOrWhiteSpace(request.City) ? null : request.City.Trim(),
            Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim(),
            Code = code,
            IsActive = request.IsActive,
            CreatedAt = DateTime.UtcNow
        };

        context.TaxOffices.Add(entity);
        await context.SaveChangesAsync(cancellationToken);

        return ToDetail(entity);
    }

    public async Task<TaxOfficeDetail> UpdateAsync(int id, TaxOfficeRequest request, CancellationToken cancellationToken = default)
    {
        await using var context = await _dbFactory.CreateDbContextAsync(cancellationToken);

        var entity = await context.TaxOffices.FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
        if (entity is null)
            throw new InvalidOperationException("Tax office not found.");

        if (string.IsNullOrWhiteSpace(request.Name))
            throw new ArgumentException("Tax office name is required.", nameof(request));

        entity.Name = request.Name.Trim();
        entity.BusinessName = string.IsNullOrWhiteSpace(request.BusinessName) ? null : request.BusinessName.Trim();
        entity.Email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim();
        entity.PhoneNumber = string.IsNullOrWhiteSpace(request.PhoneNumber) ? null : request.PhoneNumber.Trim();
        entity.ContactPerson = string.IsNullOrWhiteSpace(request.ContactPerson) ? null : request.ContactPerson.Trim();
        entity.Address = string.IsNullOrWhiteSpace(request.Address) ? null : request.Address.Trim();
        entity.PostalCode = string.IsNullOrWhiteSpace(request.PostalCode) ? null : request.PostalCode.Trim();
        entity.City = string.IsNullOrWhiteSpace(request.City) ? null : request.City.Trim();
        entity.Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim();
        entity.IsActive = request.IsActive;

        await context.SaveChangesAsync(cancellationToken);

        return ToDetail(entity);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var context = await _dbFactory.CreateDbContextAsync(cancellationToken);

        var entity = await context.TaxOffices.FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
        if (entity is null)
            return;

        context.TaxOffices.Remove(entity);
        await context.SaveChangesAsync(cancellationToken);
    }

    private static TaxOfficeDetail ToDetail(TaxOfficeEntity entity) => new(
        entity.Id,
        entity.Name,
        entity.BusinessName,
        entity.Email,
        entity.PhoneNumber,
        entity.ContactPerson,
        entity.Address,
        entity.PostalCode,
        entity.City,
        entity.Notes,
        entity.Code,
        entity.IsActive,
        entity.CreatedAt);

    public async Task<IReadOnlyList<TaxOfficeLicenseDto>> GetLicensesAsync(int taxOfficeId, CancellationToken cancellationToken = default)
    {
        await using var context = await _dbFactory.CreateDbContextAsync(cancellationToken);

        var subs = await context.UserSubscriptions
            .AsNoTracking()
            .Include(us => us.User)
            .Include(us => us.SubscriptionPlan)
            .Where(us => us.User != null && us.User.TaxOfficeId == taxOfficeId)
            .ToListAsync(cancellationToken);

        return subs.Select(us => new TaxOfficeLicenseDto(
            us.UserId,
            us.User!.RealName ?? us.User.Name,
            us.User.CustomerNumber,
            us.User.Email,
            us.SubscriptionPlan?.Name ?? "-",
            us.Status == "active",
            us.ExpirationDate,
            us.BillingInterval)).ToList();
    }

    public async Task AssignLicenseAsync(int taxOfficeId, string companyName, string? contactPerson, string? email, string? phoneNumber, int planId, string billingInterval, string? paymentMethod, CancellationToken cancellationToken = default)
    {
        await using var context = await _dbFactory.CreateDbContextAsync(cancellationToken);

        var trimmedEmail = string.IsNullOrWhiteSpace(email) ? null : email.Trim().ToLowerInvariant();

        if (trimmedEmail != null)
        {
            var emailExists = await context.Users
                .AnyAsync(u => u.Email != null && u.Email.ToLower() == trimmedEmail, cancellationToken);
            if (emailExists)
                throw new InvalidOperationException($"A user with email '{trimmedEmail}' already exists.");
        }

        var customerNumber = await CustomerNumberHelper.GenerateUniqueCustomerNumberAsync(context);
        var autoPassword = GenerateRandomPassword();
        // Username: use email if provided, otherwise derive from company name + customer number
        var username = trimmedEmail ?? $"{companyName.Trim().ToLowerInvariant().Replace(" ", ".")}.{customerNumber}";

        var user = new UserEntity
        {
            Name = username,
            RealName = string.IsNullOrWhiteSpace(contactPerson) ? companyName.Trim() : contactPerson.Trim(),
            Company = companyName.Trim(),
            Email = trimmedEmail,
            PhoneNumber = string.IsNullOrWhiteSpace(phoneNumber) ? null : phoneNumber.Trim(),
            PasswordHash = _passwordHasher.HashPassword(autoPassword),
            IsEnabled = true,
            IsDeleted = false,
            IsSpecial = false,
            CustomerNumber = customerNumber,
            TaxOfficeId = taxOfficeId,
            AcceptedPrivacyPolicy = false
        };

        context.Users.Add(user);
        await context.SaveChangesAsync(cancellationToken);

        var userRole = await context.Roles.FirstOrDefaultAsync(r => r.Name == RoleNames.User.ToRoleName(), cancellationToken);
        if (userRole != null)
        {
            context.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = userRole.Id });
            await context.SaveChangesAsync(cancellationToken);
        }

        await _subscriptionPlanService.ActivatePlanAsync(user.Id, planId, billingInterval, "Stripe", cancellationToken);

        if (!string.IsNullOrWhiteSpace(paymentMethod))
        {
            var subscription = await context.UserSubscriptions
                .Where(s => s.UserId == user.Id)
                .OrderByDescending(s => s.CreatedAt)
                .FirstOrDefaultAsync(cancellationToken);
            if (subscription != null)
            {
                subscription.PaymentMethod = paymentMethod;
                await context.SaveChangesAsync(cancellationToken);
            }
        }
    }

    private static string GenerateRandomPassword()
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghjkmnpqrstuvwxyz23456789!@#$";
        var bytes = System.Security.Cryptography.RandomNumberGenerator.GetBytes(12);
        return new string(bytes.Select(b => chars[b % chars.Length]).ToArray());
    }

    private static async Task<string> GenerateUniqueCodeAsync(IAppDbContext context, CancellationToken ct)
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        var rng = new Random();
        for (var i = 0; i < 20; i++)
        {
            var code = "STB-" + new string(Enumerable.Range(0, 4).Select(_ => chars[rng.Next(chars.Length)]).ToArray());
            if (!await context.TaxOffices.AnyAsync(t => t.Code == code, ct))
                return code;
        }
        throw new InvalidOperationException("Unable to generate a unique tax office code.");
    }
}
