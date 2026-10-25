using System.Text.RegularExpressions;
using Grimorio.Domain.Entities.Billing;
using Grimorio.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Grimorio.Infrastructure.Features.Billing;

public static partial class CustomerIdentity
{
    public static string Normalize(string? value) => Separators().Replace(value ?? "", "").ToUpperInvariant();

    public static (string? Number, TaxIdType Type) Validate(string? value, string requestedType)
    {
        if (!Enum.TryParse<TaxIdType>(requestedType, out var type) || !Enum.IsDefined(type))
            throw new InvalidOperationException("El tipo de identificacion no es valido.");
        var number = Normalize(value);
        if (type == TaxIdType.Passport)
        {
            if (!Passport().IsMatch(number)) throw new InvalidOperationException("El pasaporte debe tener entre 3 y 20 letras o numeros.");
            return (number, type);
        }
        if (number == "9999999999999") return (number, TaxIdType.FinalConsumer);
        if (number.Length == 0 && type == TaxIdType.FinalConsumer) return (null, type);
        if (Digits10().IsMatch(number))
        {
            if (!ValidCedula(number)) throw new InvalidOperationException("La cedula ecuatoriana no es valida. Revisa sus 10 digitos y el digito verificador.");
            return (number, TaxIdType.Cedula);
        }
        if (Digits13().IsMatch(number))
        {
            var third = number[2] - '0';
            if (!ValidProvince(number) || !number.EndsWith("001") || (third > 5 && third != 6 && third != 9)
                || (third < 6 && !ValidCedula(number[..10])))
                throw new InvalidOperationException("El RUC no tiene una estructura valida. Debe tener 13 digitos y terminar en 001.");
            // Corporate/foreign RUCs are not rejected by the obsolete modulo-11 rule (SRI guidance).
            return (number, TaxIdType.Ruc);
        }
        throw new InvalidOperationException("Ingresa una cedula de 10 digitos o un RUC de 13 digitos; para otros documentos selecciona Pasaporte.");
    }

    public static bool ValidCedula(string number)
    {
        if (!Digits10().IsMatch(number) || !ValidProvince(number) || number[2] > '5') return false;
        if (number.StartsWith("30") && number[2] is not ('4' or '5')) return false;
        var sum = 0;
        for (var i = 0; i < 9; i++)
        {
            var digit = (number[i] - '0') * (i % 2 == 0 ? 2 : 1);
            sum += digit > 9 ? digit - 9 : digit;
        }
        return (10 - sum % 10) % 10 == number[9] - '0';
    }

    public static async Task EnsureUnique(GrimorioDbContext db, Guid branch, string? number, TaxIdType type, Guid? self, CancellationToken ct)
    {
        if (number == null || type == TaxIdType.FinalConsumer) return;
        var existing = await db.Customers.AsNoTracking().Where(c => c.BranchId == branch && c.Id != self && c.TaxId != null)
            .Select(c => new { c.Name, c.TaxId, c.IsActive }).ToListAsync(ct);
        var duplicate = existing.FirstOrDefault(c => Normalize(c.TaxId) == number);
        if (duplicate != null)
            throw new InvalidOperationException($"Ya existe un cliente con esta identificacion: {duplicate.Name}{(duplicate.IsActive ? "" : " (inactivo)")}. Utiliza o actualiza ese registro.");
    }

    private static bool ValidProvince(string number) => int.TryParse(number[..2], out var province) && (province is >= 1 and <= 24 or 30);
    [GeneratedRegex(@"[\s-]")] private static partial Regex Separators();
    [GeneratedRegex(@"^[0-9]{10}$")] private static partial Regex Digits10();
    [GeneratedRegex(@"^[0-9]{13}$")] private static partial Regex Digits13();
    [GeneratedRegex(@"^[A-Z0-9]{3,20}$")] private static partial Regex Passport();
}
