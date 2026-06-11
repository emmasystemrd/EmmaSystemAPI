using System.ComponentModel.DataAnnotations;

namespace EmmaSystem.Application.DTOs.Cotizacion;

public sealed class CotizacionSaveDto
{
    [Required, StringLength(1)]
    public string Tipo { get; init; } = default!; // Ej: 'C' Cliente, 'P' Proveedor

    [StringLength(10)]
    public string? No_Cotizacion { get; init; } // Si es null, el SP autogenera secuencia

    [StringLength(50)]
    public string? Nombre_Cliente { get; init; }

    public DateTime Fecha { get; init; } = DateTime.Now;

    [Required]
    public int Idcliente { get; init; }

    [StringLength(500)]
    public string? Descripcion { get; init; }

    public decimal Descuento { get; init; }
    public decimal Itbis { get; init; }
    public decimal Subtotal { get; init; }
}