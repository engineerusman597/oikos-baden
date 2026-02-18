using Microsoft.EntityFrameworkCore;
using Oikos.Application.Data;
using Oikos.Application.Services.Security;
using Oikos.Application.Services.User.Models;
using Oikos.Application.Common;
using Oikos.Common.Helpers;
using Oikos.Domain.Constants;
using Oikos.Common.Constants;

namespace Oikos.Application.Services.User;

public class UserManagementService : IUserManagementService
{
    private readonly IAppDbContextFactory _dbFactory;
    private readonly IPasswordHasher _passwordHasher;

    public UserManagementService(IAppDbContextFactory dbFactory, IPasswordHasher passwordHasher)
    {
        _dbFactory = dbFactory;
        _passwordHasher = passwordHasher;
    }

    public async Task<PaginatedResult<UserDto>> GetUsersAsync(UserSearchCriteria criteria)
    {
        using var context = await _dbFactory.CreateDbContextAsync();

        var searchedUserIdList = new List<int>();
        if (criteria.RoleId.HasValue)
        {
            searchedUserIdList = await context.UserRoles
                .Where(ur => ur.RoleId == criteria.RoleId.Value)
                .Select(ur => ur.UserId)
                .Distinct()
                .ToListAsync();
        }

        if (criteria.RoleIds != null && criteria.RoleIds.Any())
        {
            var roleIds = criteria.RoleIds;
            var usersWithRoles = await context.UserRoles
                .Where(ur => roleIds.Contains(ur.RoleId))
                .Select(ur => ur.UserId)
                .Distinct()
                .ToListAsync();
            
            if (searchedUserIdList.Any())
            {
               searchedUserIdList = searchedUserIdList.Intersect(usersWithRoles).ToList();
            }
            else
            {
               searchedUserIdList = usersWithRoles;
            }
        }

        var query = context.Users.Where(u => !u.IsDeleted && !u.IsSpecial).AsQueryable();

        // Apply filters
        if (!string.IsNullOrWhiteSpace(criteria.SearchText))
        {
            var search = criteria.SearchText.Trim();
            query = query.Where(u => u.Name.Contains(search) || (u.CustomerNumber != null && u.CustomerNumber.Contains(search)));
        }

        if (!string.IsNullOrWhiteSpace(criteria.SearchRealName))
        {
            query = query.Where(u => u.RealName != null && u.RealName.Contains(criteria.SearchRealName));
        }

        if (criteria.RoleId.HasValue || (criteria.RoleIds != null && criteria.RoleIds.Any()))
        {
            query = query.Where(u => searchedUserIdList.Contains(u.Id));
        }

        // Exclude users with specific role (e.g., Admin)
        if (criteria.ExcludeRoleId.HasValue)
        {
            var excludedUserIds = await context.UserRoles
                .Where(ur => ur.RoleId == criteria.ExcludeRoleId.Value)
                .Select(ur => ur.UserId)
                .ToListAsync();
            query = query.Where(u => !excludedUserIds.Contains(u.Id));
        }

        if (criteria.PartnerId.HasValue)
        {
            query = query.Where(u => u.PartnerId == criteria.PartnerId.Value);
        }

        var activeSubscriptionUserIds = context.UserSubscriptions
            .Where(us => us.Status == SubscriptionStatusConstants.Active)
            .Select(us => us.UserId);

        if (criteria.HasActiveSubscription.HasValue)
        {
            query = criteria.HasActiveSubscription.Value
                ? query.Where(u => activeSubscriptionUserIds.Contains(u.Id))
                : query.Where(u => !activeSubscriptionUserIds.Contains(u.Id));
        }

        var totalCount = await query.CountAsync();
        var skip = (criteria.Page - 1) * criteria.PageSize;

        var users = await query
            .OrderByDescending(u => u.Id)
            .Skip(skip)
            .Take(criteria.PageSize)
            .Select(p => new
            {
                p.Id,
                p.Avatar,
                p.Name,
                p.RealName,
                p.AcademicTitle,
                p.IsEnabled,
               p.Email,
                p.CustomerNumber,
                p.PhoneNumber,
                p.Company,
                PartnerName = p.Partner != null ? p.Partner.Name : null,
                PartnerCode = p.Partner != null ? p.Partner.Code : null,
                p.SepaMandatePath,
                p.SepaMandateGeneratedAt,
                p.Gender,
                p.PrivacyAcceptedAt,
                p.AcceptedPrivacyPolicy
            })
            .ToListAsync();

        // Load roles
        var roles = await (from r in context.Roles
                          join ur in context.UserRoles on r.Id equals ur.RoleId
                          select new { r.Id, r.Name, ur.UserId }).ToListAsync();

        // Load subscriptions
        var planLookups = await context.UserSubscriptions
            .Include(us => us.SubscriptionPlan)
            .Where(us => us.Status == SubscriptionStatusConstants.Active)
            .GroupBy(us => us.UserId)
            .Select(g => new
            {
                UserId = g.Key,
                Subscription = g.OrderByDescending(x => x.ActivationDate).FirstOrDefault()
            })
            .ToDictionaryAsync(x => x.UserId, x => x.Subscription);

        var result = new List<UserDto>();
        for (var index = 0; index < users.Count; index++)
        {
            var user = users[index];
            var parsedName = NameHelper.ParseFullName(user.RealName);
            var title = string.IsNullOrWhiteSpace(user.AcademicTitle) ? parsedName.TitleKey : user.AcademicTitle;
            var firstName = parsedName.FirstName ?? string.Empty;
            var lastName = parsedName.LastName ?? string.Empty;
            var gender = NameHelper.NormalizeGender(user.Gender);

            if (string.IsNullOrWhiteSpace(firstName) && !string.IsNullOrWhiteSpace(user.RealName))
            {
                firstName = user.RealName;
                lastName = string.Empty;
            }

            var userRoles = roles.Where(r => r.UserId == user.Id).Select(r => r.Name).ToList();

            string planName = "-";
            string planExpirationDisplay = "-";
            bool hasActiveSubscription = false;

            if (planLookups.TryGetValue(user.Id, out var subscription) && subscription?.SubscriptionPlan != null)
            {
                planName = subscription.SubscriptionPlan.Name;
                planExpirationDisplay = subscription.ExpirationDate?.ToLocalTime().ToString("g") ?? "-";
                hasActiveSubscription = true;
            }

            result.Add(new UserDto
            {
                Id = user.Id,
                Number = skip + index + 1,
                Avatar = user.Avatar,
                Name = user.Name,
                RealName = user.RealName,
                Title = title,
                FirstName = firstName,
                LastName = lastName,
                Email = user.Email,
                CustomerNumber = user.CustomerNumber,
                PhoneNumber = user.PhoneNumber,
                Company = user.Company,
                PartnerName = user.PartnerName,
                PartnerCode = user.PartnerCode,
                PlanName = planName,
                PlanExpirationDisplay = planExpirationDisplay,
                SepaMandatePath = user.SepaMandatePath,
                SepaMandateGeneratedAt = user.SepaMandateGeneratedAt,
                Gender = gender,
                AcceptedPrivacyPolicy = user.AcceptedPrivacyPolicy,
                PrivacyAcceptedAt = user.PrivacyAcceptedAt,
                IsEnabled = user.IsEnabled,
                Roles = userRoles,
                HasActiveSubscription = hasActiveSubscription,
    
            });
        }

        return new PaginatedResult<UserDto>
        {
            Items = result,
            Page = criteria.Page,
            PageSize = criteria.PageSize,
            TotalCount = totalCount
        };
    }

    public async Task<UserDetailDto?> GetUserDetailAsync(int userId)
    {
        using var context = await _dbFactory.CreateDbContextAsync();

        var user = await context.Users.AsNoTracking()
            .Include(u => u.Partner)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null)
            return null;

        var roles = await (from ur in context.UserRoles
                          join role in context.Roles on ur.RoleId equals role.Id
                          where ur.UserId == userId
                          select role.Name).ToListAsync();

        var subscriptions = await context.UserSubscriptions
            .Include(us => us.SubscriptionPlan)
            .Where(us => us.UserId == userId && us.Status == SubscriptionStatusConstants.Active)
            .OrderByDescending(us => us.ActivationDate)
            .Select(us => new SubscriptionInfo
            {
                PlanName = us.SubscriptionPlan.Name,
                Status = us.Status,
                BillingInterval = us.BillingInterval,
                ActivatedAt = us.ActivationDate,
                ExpiresAt = us.ExpirationDate,
                AutoRenew = us.AutoRenew
            })
            .ToListAsync();

        var invoices = await context.Invoices
            .Include(i => i.Stage)
            .Where(i => i.UserId == userId)
            .OrderByDescending(i => i.UpdatedAt)
            .Select(i => new InvoiceInfo
            {
                Id = i.Id,
                Company = i.Company,
                Amount = i.Amount,
                Currency = i.Currency,
                StageName = i.Stage.Name,
                UpdatedAt = i.UpdatedAt,
                CreatedAt = i.CreatedAt
            })
            .ToListAsync();

        return new UserDetailDto
        {
            Id = user.Id,
            RealName = user.RealName,
            UserName = user.Name,
            Email = user.Email,
            Company = user.Company,
            CustomerNumber = user.CustomerNumber,
            PhoneNumber = user.PhoneNumber,
            IsEnabled = user.IsEnabled,
            Title = user.AcademicTitle,
            Gender = user.Gender,
            AcceptedPrivacyPolicy = user.AcceptedPrivacyPolicy,
            PrivacyAcceptedAt = user.PrivacyAcceptedAt,
            SepaMandatePath = user.SepaMandatePath,
            SepaMandateGeneratedAt = user.SepaMandateGeneratedAt,
            PartnerName = user.Partner?.Name,
            PartnerCode = user.Partner?.Code,
            Avatar = user.Avatar,
            Roles = roles,
            Subscriptions = subscriptions,
            Invoices = invoices
        };
    }

    public async Task<bool> CreateUserAsync(CreateUserRequest request)
    {
        using var context = await _dbFactory.CreateDbContextAsync();

        var normalizedEmail = request.Email?.Trim().ToLowerInvariant();
        var customerNumber = string.IsNullOrWhiteSpace(request.CustomerNumber)
            ? null
            : request.CustomerNumber.Trim().ToUpperInvariant();

        // Validate email uniqueness
        if (await context.Users.AnyAsync(u =>
            (u.Email != null && u.Email.ToLower() == normalizedEmail) ||
            (u.Name != null && u.Name.ToLower() == normalizedEmail)))
        {
            return false;
        }

        // Validate customer number uniqueness
        if (!string.IsNullOrWhiteSpace(customerNumber) &&
            await context.Users.AnyAsync(u => u.CustomerNumber != null && u.CustomerNumber == customerNumber))
        {
            return false;
        }

        // Generate customer number if not provided
        if (string.IsNullOrWhiteSpace(customerNumber))
        {
            customerNumber = await CustomerNumberHelper.GenerateUniqueCustomerNumberAsync(context);
        }

        var gender = NameHelper.NormalizeGender(request.Gender);
        var title = string.IsNullOrWhiteSpace(request.AcademicTitle) ? null : request.AcademicTitle.Trim();

        var user = new Domain.Entities.Rbac.User
        {
            IsEnabled = request.IsEnabled,
            Name = normalizedEmail!,
            RealName = request.RealName?.Trim(),
            PasswordHash = _passwordHasher.HashPassword(request.Password),
            Email = normalizedEmail,
            Gender = gender,
            AcademicTitle = title,
            PhoneNumber = request.PhoneNumber?.Trim(),
            Company = request.Company?.Trim(),
            CustomerNumber = customerNumber,
            PartnerId = request.PartnerId
        };

        context.Users.Add(user);
        await context.SaveChangesAsync();

        // Assign role
        int roleIdToAssign = request.RoleId ?? 0;
        
        // If no role specified, default to "User" role
        if (roleIdToAssign == 0)
        {
            var defaultRole = await context.Roles.FirstOrDefaultAsync(r => r.Name == RoleNames.User.ToRoleName());
            roleIdToAssign = defaultRole?.Id ?? 0;
        }

        // Assign the role if we have a valid role ID
        if (roleIdToAssign > 0)
        {
            var role = await context.Roles.FirstOrDefaultAsync(r => r.Id == roleIdToAssign);
            if (role != null)
            {
                context.UserRoles.Add(new Domain.Entities.Rbac.UserRole
                {
                    UserId = user.Id,
                    RoleId = role.Id
                });
                await context.SaveChangesAsync();
            }
        }

        return true;
    }

    public async Task<bool> UpdateUserAsync(int userId, UpdateUserRequest request)
    {
        using var context = await _dbFactory.CreateDbContextAsync();

        var user = await context.Users.FindAsync(userId);
        if (user == null)
            return false;

        user.RealName = request.RealName?.Trim();
        user.Email = request.Email?.Trim();
        user.PhoneNumber = request.PhoneNumber?.Trim();
        user.Company = request.Company?.Trim();
        var newCustomerNumber = string.IsNullOrWhiteSpace(request.CustomerNumber)
            ? null
            : request.CustomerNumber.Trim().ToUpperInvariant();

        if (string.IsNullOrWhiteSpace(newCustomerNumber))
        {
            user.CustomerNumber = await CustomerNumberHelper.GenerateUniqueCustomerNumberAsync(context);
        }
        else if (user.CustomerNumber != newCustomerNumber)
        {
            // Validate uniqueness if changed
            if (await context.Users.AnyAsync(u => u.CustomerNumber == newCustomerNumber && u.Id != userId))
            {
                return false;
            }
            user.CustomerNumber = newCustomerNumber;
        }

        user.AcademicTitle = string.IsNullOrWhiteSpace(request.AcademicTitle) ? null : request.AcademicTitle.Trim();
        user.Gender = NameHelper.NormalizeGender(request.Gender);
        user.PartnerId = request.PartnerId;

        await context.SaveChangesAsync();

        // Update role if RoleId is provided
        if (request.RoleId.HasValue)
        {
            // Remove existing roles
            var existingRoles = await context.UserRoles.Where(ur => ur.UserId == userId).ToListAsync();
            context.UserRoles.RemoveRange(existingRoles);
            
            // Add new role
            var role = await context.Roles.FirstOrDefaultAsync(r => r.Id == request.RoleId.Value);
            if (role != null)
            {
                context.UserRoles.Add(new Domain.Entities.Rbac.UserRole
                {
                    UserId = userId,
                    RoleId = role.Id
                });
                await context.SaveChangesAsync();
            }
        }

        return true;
    }

    public async Task<(bool Success, string? ErrorMessage)> DeleteUserAsync(int userId)
    {
        using var context = await _dbFactory.CreateDbContextAsync();

        var user = await context.Users.FindAsync(userId);
        if (user == null)
            return (false, "UserNotFound");

        // Check blocking constraints
        if (await context.UserSubscriptions.AsNoTracking().AnyAsync(us => us.UserId == userId))
            return (false, "HasSubscriptions");

        if (await context.Invoices.AsNoTracking().AnyAsync(i => i.UserId == userId))
            return (false, "HasInvoices");

        if (await context.InvoiceStageHistories.AsNoTracking().AnyAsync(h => h.ChangedByUserId == userId))
            return (false, "HasInvoiceHistory");

        if (await context.CompanyCheckRequests.AsNoTracking().AnyAsync(c => c.UserId == userId))
            return (false, "HasCompanyChecks");

        // Delete related records
        var userRoles = context.UserRoles.Where(ur => ur.UserId == userId);
        context.UserRoles.RemoveRange(userRoles);

        var userSettings = context.UserSettings.Where(us => us.UserId == userId);
        context.UserSettings.RemoveRange(userSettings);

        var resetTokens = context.PasswordResetTokens.Where(pr => pr.UserId == userId);
        context.PasswordResetTokens.RemoveRange(resetTokens);

        context.Users.Remove(user);

        await context.SaveChangesAsync();
        return (true, null);
    }

    public async Task<bool> ChangeUserStatusAsync(int userId, bool isEnabled)
    {
        using var context = await _dbFactory.CreateDbContextAsync();

        var user = await context.Users.FindAsync(userId);
        if (user == null)
            return false;

        user.IsEnabled = isEnabled;
        await context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ChangePasswordAsync(int userId, string newPassword)
    {
        using var context = await _dbFactory.CreateDbContextAsync();

        var user = await context.Users.FindAsync(userId);
        if (user == null)
            return false;

        user.PasswordHash = _passwordHasher.HashPassword(newPassword);
        await context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ValidateCustomerNumberAsync(string customerNumber, int? excludeUserId = null)
    {
        using var context = await _dbFactory.CreateDbContextAsync();

        var normalized = customerNumber.Trim().ToUpperInvariant();
        var query = context.Users.Where(u => u.CustomerNumber != null && u.CustomerNumber == normalized);

        if (excludeUserId.HasValue)
        {
            query = query.Where(u => u.Id != excludeUserId.Value);
        }

        return !await query.AnyAsync();
    }
    public async Task<CurrentUserDto?> GetUserAsync(int userId)
    {
        using var context = await _dbFactory.CreateDbContextAsync();
        var user = await context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null)
        {
            return null;
        }

        return new CurrentUserDto
        {
            Id = user.Id,
            Name = user.Name,
            RealName = user.RealName,
            AcademicTitle = user.AcademicTitle,
            CustomerNumber = user.CustomerNumber,
            Avatar = user.Avatar,
            Email = user.Email,
            IsEnabled = user.IsEnabled
        };
    }
}
