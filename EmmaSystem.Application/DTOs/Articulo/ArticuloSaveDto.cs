using System.ComponentModel.DataAnnotations;

namespace EmmaSystem.Application.DTOs.Articulo;

public sealed class ArticuloSaveDto
{
    [StringLength(20)]
    public string? Codigo { get; init; }

    [Required, StringLength(100)]
    public string Nombre { get; init; } = default!;

    [StringLength(256)]
    public string? Descripcion { get; init; }

    [Required, StringLength(1)]
    public string Tipo { get; init; } = "P"; // P=Producto, V=Servicio Venta, C=Servicio Compra, A=Activo Fijo, M=Material, T=Transformado, R=Cargo

    [Required]
    public int Idcategoria { get; init; }

    [Required]
    public int Idmedida { get; init; }

    public decimal Costo { get; init; }
    public decimal Precio { get; init; }
    public decimal Tax { get; init; }
    public decimal Comision { get; init; }

    // Campos específicos para Activos Fijos
    public int CategoriaAF { get; init; } // 0=No Depreciable, 1=Cat I, 2=Cat II, 3=Cat III
    public decimal Balance_Inicial { get; init; }
    public int Maximo { get; init; } // Vida útil en años
    public int Minimo { get; init; }

    public bool IsVencimiento { get; init; }
    public DateTime? Fecha_Vencimiento { get; init; }
    public DateTime? Fecha1 { get; init; } // Fecha adquisición AF

    [StringLength(10)]
    public string? Cta_Inventario { get; init; }
    [StringLength(10)]
    public string? Cta_Costo { get; init; }
    [StringLength(10)]
    public string? Cta_Ingreso { get; init; }
    [StringLength(10)]
    public string? Cta_VentaAF { get; init; }

    [StringLength(1)]
    public string Facturar_Sin_Existencia { get; init; } = "N";

    // Presentación principal (se inserta en Detalle_Producto)
    [Required]
    public decimal Unidades { get; init; } = 1;

    [StringLength(20)]
    public string? Codigo_Barra { get; init; }
}