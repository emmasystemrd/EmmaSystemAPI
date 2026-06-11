namespace EmmaSystem.Application.DTOs.Inscripcion;

public class InscripcionSaveDto
{
    // El código se genera en el SP, pero lo dejamos por si el frontend lo envía
    public string? Codigo { get; set; }
    public DateTime Fecha { get; set; }
    public int IdEstudiante { get; set; }
    public int Idcurso { get; set; }
    public DateTime? Fecha1 { get; set; }
    public DateTime? Fecha2 { get; set; }
    public int Idinstructor { get; set; }
    public int Idlogin { get; set; }
    public bool IsFacturaAutomatica { get; set; }
}