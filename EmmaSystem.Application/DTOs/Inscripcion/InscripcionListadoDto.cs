namespace EmmaSystem.Application.DTOs.Inscripcion;

public class InscripcionListadoDto
{
    public int IdInscripcion { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public DateTime? Fecha { get; set; }
    public string Estudiante { get; set; } = string.Empty;
    public string Curso { get; set; } = string.Empty;
}