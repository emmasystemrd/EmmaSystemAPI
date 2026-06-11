namespace EmmaSystem.Application.DTOs.Cliente;

/// <summary>
/// DTO completo para cargar los datos de un cliente en el formulario de edición.
/// Incluye todas las columnas que devuelve spbuscar_cliente1_codigo.
/// </summary>
public sealed class ClienteDetalleDto
{
    public int Idcliente { get; init; }
    public string Codigo { get; init; } = default!;
    public string Razon_Social { get; init; } = default!;
    public string? Nombre_Comercial { get; init; }
    public int Tipo { get; init; }
    public decimal Balance { get; init; }      // Calculado por fn_balance_cliente
    public decimal Limite { get; init; }
    public int Tipo_Id { get; init; }
    public string Num_Documento { get; init; } = default!;
    public DateTime? Fecha_Nacimiento { get; init; }
    public string? Direccion { get; init; }
    public string? Telefono { get; init; }
    public string? Email { get; init; }
    public string? Num_Cuenta { get; init; }
    public int Tipo_Ingreso { get; init; }
    public bool Tax { get; init; }
    public string Tipo_Comprobante { get; init; } = default!;
    public int Retencion_ITBIS { get; init; }
    public int Retencion_ISR { get; init; }
    public int Termino { get; init; }
    public decimal Descuento { get; init; }
    public int Forma_Pago { get; init; }
    public int Departamento { get; init; }
    public int Vendedor { get; init; }
    public int Lista_Precio { get; init; }
    public string? Comentario { get; init; }
    public int Idprovincia { get; init; }
    public int Idmunicipio { get; init; }
    public int Idsector { get; init; }
    public string Ruta { get; init; }
}