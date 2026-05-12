namespace Grimorio.Infrastructure.Services.Sri;

// Genera la clave de acceso de 49 dígitos requerida por el SRI Ecuador
// Formato: fechaEmision(8) + codDoc(2) + ruc(13) + ambiente(1) + serie(6)
//          + secuencial(9) + codigoNumerico(8) + tipoEmision(1) + verificador(1)
public static class SriKeyGenerator
{
    public static string Build(
        DateTime fechaEmision,
        string ruc,
        string ambiente,          // "1" pruebas, "2" producción
        string establecimiento,   // 3 dígitos
        string puntoEmision,      // 3 dígitos
        long secuencial,
        string codDoc = "01")     // "01" = factura
    {
        var fecha = fechaEmision.ToString("ddMMyyyy");
        var serie = establecimiento.PadLeft(3, '0') + puntoEmision.PadLeft(3, '0');
        var seq = secuencial.ToString().PadLeft(9, '0');
        var codigoNumerico = GenerateNumericCode(8);
        var partial = $"{fecha}{codDoc}{ruc}{ambiente}{serie}{seq}{codigoNumerico}1";
        return partial + Mod11(partial);
    }

    private static string GenerateNumericCode(int length)
    {
        var rng = new Random(Guid.NewGuid().GetHashCode());
        return string.Concat(Enumerable.Range(0, length).Select(_ => rng.Next(0, 10)));
    }

    private static int Mod11(string key)
    {
        int sum = 0, multiplier = 2;
        for (int i = key.Length - 1; i >= 0; i--)
        {
            sum += (key[i] - '0') * multiplier;
            multiplier = multiplier == 7 ? 2 : multiplier + 1;
        }
        int r = sum % 11;
        return r switch { 0 => 0, 1 => 1, _ => 11 - r };
    }
}
