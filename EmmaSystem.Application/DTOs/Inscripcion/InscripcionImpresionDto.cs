namespace EmmaSystem.Application.DTOs.Inscripcion;

public class InscripcionImpresionDto
{
    public string Codigo_Inscripcion { get; set; } = string.Empty;
    public DateTime? Fecha { get; set; }
    public string Estudiante { get; set; } = string.Empty;
    public string Curso { get; set; } = string.Empty;
    public string Nivel { get; set; } = string.Empty;
    public DateTime? Fecha1 { get; set; }
    public DateTime? Fecha2 { get; set; }
    public string Horario { get; set; } = string.Empty;
    public decimal Valor_Inscripcion { get; set; }
    public decimal Valor_Mensual { get; set; }
    public byte[]? Foto { get; set; }
    public string Codigo_Estudiante { get; set; } = string.Empty;
    public string Nombres { get; set; } = string.Empty;
    public string Apellido1 { get; set; } = string.Empty;
    public string Apellido2 { get; set; } = string.Empty;
    public string Tipo_Documento { get; set; } = string.Empty;
    public string Num_Documento { get; set; } = string.Empty;
    public string Lugar_Nacimiento { get; set; } = string.Empty;
    public string Nacionalidad { get; set; } = string.Empty;
    public string Tipo_Sangre { get; set; } = string.Empty;
    public string Sexo { get; set; } = string.Empty;
    public string Telefono { get; set; } = string.Empty;
    public string Celular { get; set; } = string.Empty;
    public DateTime? Fecha_Nacimiento { get; set; }
    public string Provincia { get; set; } = string.Empty;
    public string Municipio { get; set; } = string.Empty;
    public string Sector { get; set; } = string.Empty;
    public string Direccion { get; set; } = string.Empty;
    public string Alergico { get; set; } = string.Empty;
    public string Medicamentos_que_usa { get; set; } = string.Empty;
    public string Padre { get; set; } = string.Empty;
    public string Cedula_Padre { get; set; } = string.Empty;
    public string Parentesco { get; set; } = string.Empty;
    public string Telefono_Padre { get; set; } = string.Empty;
    public int Idlogin { get; set; }
    public string Doc_Instructor { get; set; } = string.Empty;
    public string Instructor { get; set; } = string.Empty;
}