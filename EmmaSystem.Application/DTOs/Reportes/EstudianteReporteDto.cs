namespace EmmaSystem.Application.DTOs.Reportes;

public class EstudianteReporteDto
{
    public int IdEstudiante { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Estudiante { get; set; } = string.Empty;
    public string Num_Documento { get; set; } = string.Empty;
    public string Telefono { get; set; } = string.Empty;
    public string Celular { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public DateTime? Fecha_Nacimiento { get; set; }
    public string Sexo { get; set; } = string.Empty;
    public string? Provincia { get; set; }
    public string? Municipio { get; set; }
    public string? Sector { get; set; }
    public string? Tutor { get; set; }
    public string Parentesco { get; set; } = string.Empty;
    // ✅ Estado del estudiante como persona
    public string EstadoEstudianteDescripcion { get; set; } = string.Empty;
    public string EstadoEstudiante { get; set; } = string.Empty;
    // ✅ NUEVO: Estado de la inscripción (si se filtra por curso)
    public string? EstadoInscripcionDescripcion { get; set; }
    public string? EstadoInscripcion { get; set; }
    public string? NombreCurso { get; set; }
}