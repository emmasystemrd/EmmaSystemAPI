namespace EmmaSystem.Application.DTOs.Articulo;

public sealed class ArticuloDto
{
    public int Idarticulo { get; init; }
    public string Codigo { get; init; } = default!;
    public string Nombre { get; init; } = default!;
    public string? Descripcion { get; init; }
    public DateTime? Fecha1 {  get; init; } = default!;
    public int Idcategoria { get; init; }
    public int Idmedida { get; init; }
    public string Tipo { get; init; } = default!; // P, V, C, A, M, T, R
    public decimal Costo { get; init; }
    public decimal Precio { get; init; }
    public decimal Comision { get; init; }
    public decimal Tax { get; init; }
    public bool IsVencimiento { get; init; }
    public DateTime? Fecha_Vencimiento { get; init; }
    public string Cta_Inventario { get; init; }
    public string Cta_Costo { get; init; }
    public string Cta_Ingreso { get; init; }
    public string Cta_VentaAF { get; init; }
    public int? Categoria { get; init; } = default!;
    //public string Medida { get; init; } = default!;
    // ✅ CAMBIO: De decimal a string porque el SP devuelve '92.00 UNIDAD'
    public string Existencia { get; init; } = default!;
    public decimal Exist { get; init; } 
    public decimal Maximo { get; init; }
    public decimal Minimo { get; init; }
    public string Facturar_Sin_Existencia { get; init; }
    public string Estado { get; init; } = default!;
    public string? FotoBase64 { get; set; }
}