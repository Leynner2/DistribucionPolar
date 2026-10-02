namespace Core.Application.DTOs;

/// <summary>Reporte diario / cierre de caja. Excluye documentos anulados.</summary>
/// <param name="Date">Día local de la empresa.</param>
/// <param name="SalesTotal">Ventas del día (suma de totales de facturas de venta).</param>
/// <param name="SalesCount">Número de facturas de venta.</param>
/// <param name="GiftsCount">Número de regalías.</param>
/// <param name="CollectedOnInvoices">Cobrado al facturar (suma de abonos de facturas).</param>
/// <param name="CreditGenerated">Fiado generado (suma de pendientes).</param>
/// <param name="PaymentsTotal">Abonos del día (incluye los abonos al facturar).</param>
/// <param name="EmptiesGeneratedBoxes">Cajas de vacíos generadas.</param>
/// <param name="EmptiesGeneratedUnits">Unidades de vacíos generadas.</param>
/// <param name="EmptiesReturnedBoxes">Cajas de vacíos devueltas en facturas.</param>
/// <param name="EmptiesReturnedUnits">Unidades de vacíos devueltas en facturas.</param>
/// <param name="InternalConsumptionValue">Valor del consumo interno del día.</param>
/// <param name="DamageLoss">Pérdida por averías del día.</param>
public sealed record DailyReportResponse(
    DateOnly Date, decimal SalesTotal, int SalesCount, int GiftsCount, decimal CollectedOnInvoices, decimal CreditGenerated,
    decimal PaymentsTotal, int EmptiesGeneratedBoxes, int EmptiesGeneratedUnits, int EmptiesReturnedBoxes, int EmptiesReturnedUnits,
    decimal InternalConsumptionValue, decimal DamageLoss);

/// <summary>Balance general.</summary>
/// <param name="MoneyOnStreet">Dinero en la calle: suma de deudas de clientes.</param>
/// <param name="EmptyBoxesOnStreet">Cajas de vacíos adeudadas.</param>
/// <param name="EmptyUnitsOnStreet">Unidades de vacíos adeudadas.</param>
/// <param name="ReceivableClients">Clientes con alguna deuda.</param>
/// <param name="Inventory">Inventario de galpón y camiones.</param>
/// <param name="ConsignmentReceivable">Por cobrar en consignaciones.</param>
public sealed record BalanceResponse(
    decimal MoneyOnStreet, int EmptyBoxesOnStreet, int EmptyUnitsOnStreet, int ReceivableClients,
    InventorySummaryResponse Inventory, decimal ConsignmentReceivable);
