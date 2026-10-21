using System.ComponentModel.DataAnnotations;

namespace EmmaSystem.Application.DTOs.Cliente;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public sealed class OptionalEmailAddressAttribute : ValidationAttribute
{
    private static readonly EmailAddressAttribute Inner = new();

    public OptionalEmailAddressAttribute()
        : base("El correo electrónico no tiene un formato válido.") { }

    public override bool IsValid(object? value)
    {
        if (value is null) return true;
        if (value is not string s) return false;
        return string.IsNullOrWhiteSpace(s) || Inner.IsValid(s.Trim());
    }
}