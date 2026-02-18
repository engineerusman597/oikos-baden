using Oikos.Domain.Enums;

namespace Oikos.Application.Services.Dashboard.Models;

public record DashboardStatusSummaryDto(InvoicePrimaryStatus PrimaryStatus, int Count);
