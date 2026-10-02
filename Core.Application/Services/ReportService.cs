using Core.Application.Abstractions;
using Core.Application.DTOs;

namespace Core.Application.Services;

public sealed class ReportService(
    IReportQueries reports,
    IConsignmentService consignments,
    IAuditLogRepository auditLogs,
    IClock clock) : IReportService
{
    /// <summary>Reporte diario del día local indicado (hoy por defecto). Excluye documentos anulados.</summary>
    public Task<DailyReportResponse> GetDailyReportAsync(DateOnly? date, CancellationToken cancellationToken = default)
    {
        var day = date ?? clock.Today;
        var (startUtc, endUtc) = clock.GetDayRangeUtc(day);
        return reports.GetDailyReportAsync(day, startUtc, endUtc, cancellationToken);
    }

    /// <summary>Balance general: dinero y vacíos en la calle, inventario total y consignación por cobrar.</summary>
    public async Task<BalanceResponse> GetBalanceAsync(CancellationToken cancellationToken = default)
    {
        var street = await reports.GetStreetBalancesAsync(cancellationToken);
        var inventory = await reports.GetInventorySummaryAsync(cancellationToken);
        var consignment = await consignments.GetIndicatorsAsync(cancellationToken);
        return new BalanceResponse(
            street.MoneyOnStreet, street.EmptyBoxesOnStreet, street.EmptyUnitsOnStreet, street.ReceivableClients,
            inventory, consignment.ReceivableTotal);
    }

    public Task<PagedResponse<AuditLogResponse>> GetAuditLogAsync(
        DateOnly? from, DateOnly? to, string? module, string? username, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        DateTime? fromUtc = from is null ? null : clock.GetDayRangeUtc(from.Value).StartUtc;
        DateTime? toUtc = to is null ? null : clock.GetDayRangeUtc(to.Value).EndUtc;
        return auditLogs.ListAsync(fromUtc, toUtc, module, username, Math.Max(1, page), Math.Clamp(pageSize, 1, 200), cancellationToken);
    }
}
