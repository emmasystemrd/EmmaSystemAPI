using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace EmmaSystem.Application.DTOs.Proveedor;
public sealed class ProveedorDto
{
    public int Idproveedor { get; init; }
    public string Codigo { get; init; } = default!;
    public string Razon_Social { get; init; } = default!;
    public string Nombre_Comercial { get; init; } = default!;
    public int Tipo { get; init; }
    public int Tipo_Id { get; init; }
    public string Num_Documento { get; init; } = default!;
    public string Direccion { get; init; } = default!;
    public string Telefono { get; init; } = default!;
    public string Email { get; init; } = default!;
    public int Tipo_B_S { get; init; }
    public decimal Limite { get; init; }
    public decimal Balance { get; init; }
    public string Estado { get; init; } = default!;
}