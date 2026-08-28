using System.ComponentModel.DataAnnotations;

namespace EmmaSystem.Application.DTOs.Estudiante;

public class EstudianteSaveDto : IValidatableObject
{
    public string Codigo { get; set; } = string.Empty;
    public string Nombres { get; set; } = string.Empty;
    public string Apellido1 { get; set; } = string.Empty;
    public string Apellido2 { get; set; } = string.Empty;
    public string Sexo { get; set; } = string.Empty;
    public byte[]? Foto { get; set; }
    public int Tipo_Doc { get; set; }
    public string Num_Documento { get; set; } = string.Empty;
    public int? Lugar_Nacimiento { get; set; }
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

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Fecha_Nacimiento.HasValue)
        {
            var edad = CalcularEdad(Fecha_Nacimiento.Value);

            if (edad >= 18 && Tipo_Doc == 3)
            {
                yield return new ValidationResult(
                    "El estudiante mayor de edad no puede tener Acta de Nacimiento. Seleccione Cédula o Pasaporte.",
                    new[] { nameof(Tipo_Doc) });
            }

            if (edad >= 18 && (Tipo_Doc == 1 || Tipo_Doc == 2) && string.IsNullOrWhiteSpace(Num_Documento))
            {
                yield return new ValidationResult(
                    "El número de documento es obligatorio para estudiantes mayores de edad.",
                    new[] { nameof(Num_Documento) });
            }
        }
    }

    private static int CalcularEdad(DateTime fechaNacimiento)
    {
        var hoy = DateTime.Today;
        var edad = hoy.Year - fechaNacimiento.Year;

        if (fechaNacimiento.Date > hoy.AddYears(-edad))
            edad--;

        return edad;
    }
}