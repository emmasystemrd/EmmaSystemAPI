namespace EmmaSystem.Application.DTOs.Estudiante;

public class EstudianteListadoDto
{
    public int IdEstudiante { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Estudiante { get; set; } = string.Empty;
    public string Num_Documento { get; set; } = string.Empty;
    public DateTime? Fecha_Nacimiento { get; set; }
    public string Telefono { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
}