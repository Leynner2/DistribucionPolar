using Core.Application.DTOs;

namespace Core.Application.Abstractions;

/// <summary>
/// Consultas de solo lectura para reportes: agregaciones calculadas en PostgreSQL y proyectadas
/// directamente a DTO, sin materializar entidades.
/// </summary>
public interface IReportQueries
{
    Task<DailyReportResponse> GetDailyReportAsync(DateOnly date, DateTime startUtc, DateTime endUtc, CancellationToken cancellationToken = default);

    Task<InventorySummaryResponse> GetInventorySummaryAsync(CancellationToken cancellationToken = default);

    Task<(decimal MoneyOnStreet, int EmptyBoxesOnStreet, int EmptyUnitsOnStreet, int ReceivableClients)> GetStreetBalancesAsync(CancellationToken cancellationToken = default);

    Task<ProductLocationResponse?> GetProductLocationAsync(Guid productId, CancellationToken cancellationToken = default);
}
