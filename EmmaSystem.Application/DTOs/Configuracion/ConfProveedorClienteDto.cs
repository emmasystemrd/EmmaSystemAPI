namespace EmmaSystem.Application.DTOs.Configuracion;

public class ConfProveedorClienteDto
{
    public int Id { get; set; }
    public string? Capital { get; set; }
    public string? Cuenta_Cierre { get; set; }

    // Proveedor
    public string? Idproveedor { get; set; }
    public string? CxP { get; set; }
    public string? Dev_Compra { get; set; }
    public string? Desc_Compra { get; set; }
    public string? GastoI { get; set; }

    // Clientes
    public string? Idcliente { get; set; }
    public string? Caja { get; set; }
    public string? CxC_Cia { get; set; }
    public string? CxC { get; set; }
    public string? Devoluciones { get; set; }
    public string? Descuentos { get; set; }
    public string? IngresoI { get; set; }
    public bool? Cliente_Cumple { get; set; }
}