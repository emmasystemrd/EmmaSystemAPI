namespace EmmaSystem.Application.DTOs.Estudiante;

public class EstudianteSaveDto
{
    public string Codigo { get; set; } = string.Empty;
    public string Nombres { get; set; } = string.Empty;
    public string Apellido1 { get; set; } = string.Empty;
    public string Apellido2 { get; set; } = string.Empty;
    public string Sexo { get; set; } = string.Empty;
    public byte[]? Foto { get; set; }
    public int Tipo_Doc { get; set; }
    public string Num_Documento { get; set; } = string.Empty;
    public string Lugar_Nacimiento { get; set; } = string.Empty;
    public string Telefono { get; set; } = string.Empty;
    public string Celular { get; set; } = string.Empty;
    public int? Nacionalidad { get; set; }
    public DateTime? Fecha_Nacimiento { get; set; }
    public string Tipo_Sangre { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Direccion { get; set; } = string.Empty;
    public int? Provincia { get; set; }
    public int? Municipio { get; set; }
    public long? Sector { get; set; }
    public string Alergico { get; set; } = string.Empty;
    public string Medicamentos_que_usa { get; set; } = string.Empty;
    public int? Idcliente { get; set; }
    public int? Parentesco { get; set; }
    public string Estado { get; set; } = "A";
}