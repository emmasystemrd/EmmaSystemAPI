namespace EmmaSystem.Application.DTOs.Cliente;

public sealed class ClienteDto
{
    // ═══ IDENTIFICACIÓN ═══
    public int Idcliente { get; init; }
    public string Codigo { get; init; } = default!;
    public string Razon_Social { get; init; } = default!;
    public string Nombre_Comercial { get; init; } = default!;
    public int Tipo { get; init; }                    // 1=Jurídica, 2=Física, 3=Consumidor Final
    public int Tipo_Id { get; init; }                 // 1=RNC, 2=Cédula, 3=Pasaporte, 4=ID Tributaria, 5=Sin Doc
    public string Num_Documento { get; init; } = default!;
    public DateTime? Fecha_Nacimiento { get; init; }  // Nullable porque puede no tener valor
    public string Estado { get; init; } = default!;   // 'A'=Activo, 'I'=Inactivo

    // ═══ CONTACTO Y UBICACIÓN ═══
    public string Direccion { get; init; } = default!;
    public string Telefono { get; init; } = default!;
    public string Email { get; init; } = default!;
    public int Idprovincia { get; init; }
    public int Idmunicipio { get; init; }
    public int Idsector { get; init; }
    public string Ruta { get; init; } = default!;

    // ═══ FINANCIERO ═══
    public decimal Limite { get; init; }
    public decimal Balance { get; init; }             // Calculado por fn_balance_cliente
    public string Num_Cuenta { get; init; } = default!;  // Cuenta contable CxC
    public int Termino { get; init; }                 // 1=Contado, 2=30 días, 3=60 días, 4=90 días
    public decimal Descuento { get; init; }
    public int Forma_Pago { get; init; }              // 0=Ninguno, 1=Efectivo, 2=Cheque, 3=Transferencia, 4=Tarjeta
    public int Lista_Precio { get; init; }            // 1=General, 2=Mayorista, 3=Detallista

    // ═══ FISCAL / TRIBUTARIO ═══
    public int Tipo_Ingreso { get; init; }            // 1=Operaciones, 2=Financieros, 3=Extraordinarios, etc.
    public bool Tax { get; init; }                    // Aplica ITBIS
    public string Tipo_Comprobante { get; init; } = default!;  // '01', '02', '31', '32', etc.
    public decimal Retencion_ITBIS { get; init; }
    public decimal Retencion_ISR { get; init; }

    // ═══ ASIGNACIONES COMERCIALES ═══
    public int Departamento { get; init; }            // Centro de costos
    public int Vendedor { get; init; }                // Vendedor asignado

    // ═══ OBSERVACIONES ═══
    public string Comentario { get; init; } = default!;
}