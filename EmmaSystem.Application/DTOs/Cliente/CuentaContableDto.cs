namespace EmmaSystem.Application.DTOs.Cliente;

/// <summary>
/// DTO para cuentas contables del catálogo (solo tipo 'D' - Detalle).
/// Mapea el resultado de [dbo].[spbuscar_cuenta_detalle].
/// </summary>
public sealed class CuentaContableDto
{
    public string Num_Cuenta { get; init; } = default!;
    public string Nombre { get; init; } = default!;
    public string? Descripcion { get; init; }
    public string? Grupo { get; init; }
    public string Tipo { get; init; } = default!;  // Siempre 'D' (Detalle)
    public string? Cod_Padre { get; init; }
    public string? Padre { get; init; }
    public int Nivel { get; init; }
}