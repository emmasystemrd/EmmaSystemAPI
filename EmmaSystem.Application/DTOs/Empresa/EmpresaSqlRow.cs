// Clase interna solo para mapeo de SQL
public class EmpresaSqlRow
{
    public int Idempresa { get; set; }
    public string? Nombre { get; set; }
    public string? Tipo { get; set; }
    public string? RNC { get; set; }
    public string? Direccion { get; set; }
    public string? Telefono { get; set; }
    public string? Email { get; set; }
    public string? Url { get; set; }
    public string? Instagram { get; set; }
    public string? Facebook { get; set; }
    public bool Registrado { get; set; }
    public DateTime? Fecha_Cierre { get; set; } // ✅ DateTime? maneja NULL correctamente
    public int TieneLogo { get; set; }
}