namespace DePaso.Web.Models
{
    public class UsuarioApiResponse
    {
        public int Id { get; set; }
        public string Email { get; set; } = "";
        public string Nombre { get; set; } = "";
        public string Apellido { get; set; } = "";
        public DateTime? CreadoEn { get; set; }
    }
}
