using System.Net;
using System.Net.Http.Json;

namespace DePaso.Tests.envios;

internal sealed class EnviosHttpFalso(HttpStatusCode estado, object? datos = null) : HttpMessageHandler
{
    public HttpMethod? Metodo { get; private set; }

    public string? Ruta { get; private set; }

    public string? Cuerpo { get; private set; }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage solicitud, CancellationToken cancelacion)
    {
        Metodo = solicitud.Method;
        Ruta = solicitud.RequestUri?.PathAndQuery;
        Cuerpo = solicitud.Content is null
            ? null
            : await solicitud.Content.ReadAsStringAsync(cancelacion);

        return new HttpResponseMessage(estado)
        {
            Content = datos is null ? null : JsonContent.Create(datos)
        };
    }
}
