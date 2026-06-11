namespace EmmaSystem.Application.DTOs.Venta;

/// <summary>
/// DTO para información de pago de venta (spventa_pagada).
/// </summary>
public sealed class VentaPagoDto
{
    public decimal Efectivo { get; init; }
    public decimal Cheque { get; init; }
    public string Banco_Ck { get; init; } = default!;
    public string Num_Ck { get; init; } = default!;
    public decimal Transferencia { get; init; }
    public string Banco_Transf { get; init; } = default!;
    public string Ref_Transf { get; init; } = default!;
    public decimal Tarjeta { get; init; }
    public string Tipo_Tarjeta { get; init; } = default!;
    public string Ref_Tarjeta { get; init; } = default!;
    public int? Idretencion_ITBIS { get; init; }
    public decimal? Tasa_Ret_ITBIS { get; init; }
    public decimal Retencion_ITBIS { get; init; }
    public int? Idretencion_ISR { get; init; }
    public decimal? Tasa_Ret_ISR { get; init; }
    public decimal Retencion_ISR { get; init; }
    public decimal Devuelta { get; init; }
    public string Codigo { get; init; } = default!;
}