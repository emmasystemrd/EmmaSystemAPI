namespace EmmaSystem.Application.DTOs.Inscripcion;

public class InscripcionDetalleDto
{
    public int IdInscripcion { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Codigo_Curso { get; set; } = string.Empty;
    public string Nivel { get; set; } = string.Empty;
    public bool IsFacturaAutomatica { get; set; }
    public decimal Valor_Inscripcion { get; set; }
    public decimal Valor_Mensual { get; set; }
    public DateTime? Fecha { get; set; }
    public DateTime? Fecha1 { get; set; }
    public DateTime? Fecha2 { get; set; }
    public string Horario { get; set; } = string.Empty;
    public string Codigo_Estudiante { get; set; } = string.Empty;
    public string Codigo_Facilitador { get; set; } = string.Empty;
}