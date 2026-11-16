using DePaso.Logica.dtos;
using System.Globalization;
using System.Text;

namespace DePaso.Logica.helpers;

public static class TextoNormalizado
{
    public static string Normalizar(string? texto)
    {
        if (string.IsNullOrWhiteSpace(texto))
        {
            return string.Empty;
        }

        var descompuesto = texto.Trim().Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(descompuesto.Length);
        var ultimoEspacio = false;
        foreach (var c in descompuesto)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            if (char.IsWhiteSpace(c))
            {
                if (!ultimoEspacio)
                {
                    sb.Append(' ');
                }

                ultimoEspacio = true;
                continue;
            }

            ultimoEspacio = false;
            sb.Append(char.ToUpperInvariant(c));
        }

        return sb.ToString().Normalize(NormalizationForm.FormC);
    }

    public static string Clave(string? calle, int altura, string? localidad, string? provincia) =>
        $"{Normalizar(calle)}|{altura}|{Normalizar(localidad)}|{Normalizar(provincia)}";

    public static string ClaveDireccion(DireccionDatos d) => Clave(d.Calle, d.Altura, d.Localidad, d.Provincia);
}