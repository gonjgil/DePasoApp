using System.Net;
using System.Net.Http.Json;
using DePaso.Web.Models;
using Microsoft.AspNetCore.Identity;

namespace DePaso.Web.Services;

    public class UsuariosApiClient
    {
        private readonly HttpClient _httpClient;

        public UsuariosApiClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<UsuarioApiResponse?> IngresarAsync(
            string email,
            string password)
        {
            var respuesta = await _httpClient.PostAsJsonAsync(
                "api/usuarios/ingresar",
                new
                {
                    Email = email,
                    password = password
                });

            if (respuesta.StatusCode == HttpStatusCode.Unauthorized)
            {
                return null;
            }
            respuesta.EnsureSuccessStatusCode();

            return await respuesta.Content.ReadFromJsonAsync<UsuarioApiResponse>();
        }

        public async Task<(UsuarioApiResponse? Usuario, string? Error)> RegistrarseAsync(
            string nombre,
            string apellido,
            string email,
            string password)
            {
            var respuesta = await _httpClient.PostAsJsonAsync(
                "api/usuarios/registrarse",
                new
                {
                    Nombre = nombre,
                    Apellido = apellido,
                    Email = email,
                    Password = password
                });

            if (respuesta.StatusCode == HttpStatusCode.Conflict)
            {
                var error = await respuesta.Content.ReadFromJsonAsync<ErrorApiResponse>();

            return (null, error?.Mensaje ?? "El email ya está registrado.");
            }

            if (!respuesta.IsSuccessStatusCode)
            {
                return (null, "No se pudo completar el registro.");
            }

            var usuario = await respuesta.Content.ReadFromJsonAsync<UsuarioApiResponse>();

            return (usuario, null);
        }
    }
