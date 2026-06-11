namespace EmmaSystem.Application.DTOs.Dgii;

public class ContribuyenteDto
{
    public string RncCedula { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string NombreComercial { get; set; } = string.Empty;
    public string Categoria { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
    public string TipoContribuyente { get; set; } = string.Empty;
    public string RegimenPagos { get; set; } = string.Empty;
}

public class RncRegistradoDto
{
    public string RncOCedula { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string NombreComercial { get; set; } = string.Empty;
    public string ActividadEconomica { get; set; } = string.Empty;
    public string TipoContribuyente { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
}

public class NcfValidationDto
{
    public string Ncf { get; set; } = string.Empty;
    public string RncEmisor { get; set; } = string.Empty;
    public string TipoComprobante { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
    public string RncComprador { get; set; } = string.Empty;
    public string NombreComprador { get; set; } = string.Empty;
    public decimal MontoTotal { get; set; }
    public string FechaEmision { get; set; }
}

public class ENcfValidationDto
{
    public string Estado { get; set; } = string.Empty;
    public string FechaEmision { get; set; }
    public decimal MontoTotal { get; set; }
}