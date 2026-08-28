namespace EmmaSystem.Application.DTOs.Estudiante;

public class EstudianteBusquedaDto
{
    public int Idestudiante { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Estudiante { get; set; } = string.Empty;
    public string Num_Documento { get; set; } = string.Empty;
    public string Telefono { get; set; } = string.Empty;
    public string Codigo_Padre { get; set; } = string.Empty;
}