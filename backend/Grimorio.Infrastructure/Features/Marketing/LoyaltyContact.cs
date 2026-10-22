using System.Net.Mail;
using Grimorio.Application.DTOs;
using Grimorio.Domain.Entities.Marketing;

namespace Grimorio.Infrastructure.Features.Marketing;

public static class LoyaltyContact
{
    public static string? NormalizePhone(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length > 40) return null;
        if (value.Any(c => !(c is >= '0' and <= '9' or '+' or ' ' or '-' or '(' or ')' or '.'))) return null;
        var compact = string.Concat(value.Where(c => c is >= '0' and <= '9' or '+'));
        if (compact.StartsWith("00")) compact = "+" + compact[2..];
        if (compact.Length == 10 && compact.StartsWith("09")) compact = "+593" + compact[1..];
        else if (compact.Length == 12 && compact.StartsWith("593")) compact = "+" + compact;
        if (compact.Length is < 9 or > 16 || compact[0] != '+' || compact[1] == '0'
            || compact[1..].Any(c => c is < '0' or > '9')) return null;
        if (compact.StartsWith("+593") && (compact.Length != 13 || compact[4] != '9')) return null;
        return compact;
    }

    internal static void Apply(LoyaltyMember member, LoyaltyMemberProfileDto data)
    {
        var name = data.Name?.Trim();
        if (string.IsNullOrWhiteSpace(name) || name.Length > 200)
            throw new InvalidOperationException("Ingresa el nombre del miembro (maximo 200 caracteres).");
        var phone = NormalizePhone(data.Phone) ?? throw new InvalidOperationException("Ingresa un celular ecuatoriano de 10 digitos o un numero internacional con + y codigo de pais.");
        var email = string.IsNullOrWhiteSpace(data.Email) ? null : data.Email.Trim();
        if (email is not null && (email.Length > 254 || !MailAddress.TryCreate(email, out var address) || address.Address != email))
            throw new InvalidOperationException("El correo no es valido.");
        if (data.BirthDay.HasValue != data.BirthMonth.HasValue)
            throw new InvalidOperationException("Completa tanto el dia como el mes del cumpleanos, o deja ambos vacios.");
        if (data.BirthMonth.HasValue && (data.BirthMonth is < 1 or > 12 || data.BirthDay < 1
            || data.BirthDay > DateTime.DaysInMonth(2000, data.BirthMonth.Value)))
            throw new InvalidOperationException("El dia y mes del cumpleanos no son validos.");
        if (member.Phone != phone) member.PhoneVerifiedAt = null;
        member.Name = name; member.Phone = phone; member.Email = email;
        member.BirthDay = data.BirthDay; member.BirthMonth = data.BirthMonth;
        if (member.MarketingConsent != data.MarketingConsent) member.MarketingConsentUpdatedAt = DateTime.UtcNow;
        member.MarketingConsent = data.MarketingConsent;
    }
}
