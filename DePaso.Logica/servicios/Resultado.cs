namespace DePaso.Logica.servicios;

public enum TipoError
{
    Validacion,
    NoEncontrado,
    Prohibido,
    Conflicto,
}

public sealed class Resultado<T>
{
    private Resultado(T? valor, TipoError? error, string? mensaje, IReadOnlyList<string> errores)
    {
        Valor = valor;
        Error = error;
        Mensaje = mensaje;
        Errores = errores;
    }

    public T? Valor { get; }
    public TipoError? Error { get; }
    public string? Mensaje { get; }
    public IReadOnlyList<string> Errores { get; }
    public bool Exito => Error is null;

    public static Resultado<T> Exitoso(T valor) => new(valor, null, null, Array.Empty<string>());

    public static Resultado<T> Fallo(TipoError error, string mensaje) =>
        new(default, error, mensaje, new[] { mensaje });

    public static Resultado<T> Invalido(IReadOnlyList<string> errores) =>
        new(default, TipoError.Validacion, string.Join(" ", errores), errores);

    public static Resultado<T> Invalido(string error) => Invalido(new[] { error });

    public Resultado<TOtro> Propagar<TOtro>() =>
        Exito
            ? throw new InvalidOperationException("Sólo se pueden propagar resultados con error.")
            : Resultado<TOtro>.Crear(Error!.Value, Mensaje!, Errores);

    private static Resultado<T> Crear(TipoError error, string mensaje, IReadOnlyList<string> errores) =>
        new(default, error, mensaje, errores);
}
