namespace EmmaSystem.Application.DTOs.Estudiante;

public class EstudianteCalificacionDto
{
    public int IdInscripcion { get; set; } // ✅ AHORA se actualiza la inscripción, no el estudiante
    public int IdEstudiante { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Estudiante { get; set; } = string.Empty;
    public string Num_Documento { get; set; } = string.Empty;
    public string Telefono { get; set; } = string.Empty;
    public string EstadoDescripcion { get; set; } = string.Empty;
    public string EstadoInscripcion { get; set; } = string.Empty;
    public DateTime? FechaInscripcion { get; set; }
    public string NombreCurso { get; set; } = string.Empty;
}

public class ActualizarEstadoInscripcionDto
{
    public int IdInscripcion { get; set; } // ✅ Cambiado de IdEstudiante a IdInscripcion
    public string NuevoEstado { get; set; } = string.Empty;
}