using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.ComponentModel.DataAnnotations;

namespace EmmaSystem.Application.DTOs.Cliente;

public sealed class ClienteSaveDto
{
    [StringLength(10)]
    public string? Codigo { get; init; } // Necesario para el SP de editar

    [Required, StringLength(50)]
    public string Razon_Social { get; init; } = default!;

    [StringLength(50)]
    public string? Nombre_Comercial { get; init; }

    [Required]
    public int Tipo { get; init; } // 1=Jurídica, 2=Física, 3=Informal

    [Required]
    public int Tipo_Id { get; init; } // 1=RNC, 2=Cédula, 3=Pasaporte

    [Required, StringLength(11)]
    public string Num_Documento { get; init; } = default!;

    [StringLength(100)]
    public string? Direccion { get; init; }

    [StringLength(10)]
    public string? Telefono { get; init; }

    [StringLength(50), EmailAddress]
    public string? Email { get; init; }

    public int Tipo_Ingreso { get; init; } = 1;
    public bool Tax { get; init; } = true;

    [StringLength(2)]
    public string Tipo_Comprobante { get; init; } = "02";

    public int Retencion_ITBIS { get; init; } = 0;
    public int Retencion_ISR { get; init; } = 0;

    public decimal Limite { get; init; } = 0;
    public decimal Balance { get; init; } = 0; // Si es > 0, el trigger contable generará el asiento de apertura
    public DateTime? Fecha1 { get; init; }

    [StringLength(10)]
    public string? Num_Cuenta { get; init; }

    public int Termino { get; init; } = 1;
    public decimal Descuento { get; init; } = 0;
    public int Forma_Pago { get; init; } = 0;
    public int Departamento { get; init; } = 0;
    public int Vendedor { get; init; } = 0;
    public int Lista_Precio { get; init; } = 1;

    [StringLength(256)]
    public string? Comentario { get; init; }

    public int Idprovincia { get; init; } = 0;
    public int Idmunicipio { get; init; } = 0;
    public int Idsector { get; init; } = 0;

    [StringLength(50)]
    public string? Ruta { get; init; }
}