namespace EmmaSystem.Application.DTOs.Asistencia;

public class AsistenciaReporteFiltradoDto
{
    public string Estudiante { get; set; } = string.Empty;
    public string Sexo { get; set; } = string.Empty;
    public int Edad { get; set; }
    public string Telefono { get; set; } = string.Empty;
    public string Curso { get; set; } = string.Empty;
    public string Instructor { get; set; } = string.Empty;
    public DateTime FechaAsistencia { get; set; }
    public string Asistencia { get; set; } = string.Empty;
}