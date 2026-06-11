namespace EmmaSystem.Application.DTOs.Asistencia;

public class EstudianteAsistenciaDto
{
    public int? Iddetalle { get; set; }
    public int Idestudiante { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Estudiante { get; set; } = string.Empty;
    public string Asistencia { get; set; } = string.Empty; // "PRESENTE", "AUSENTE", o "" si es nuevo
}