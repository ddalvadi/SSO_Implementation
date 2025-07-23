namespace OpenIddict_Server.Models
{
    public class AppRedirectUri
    {
        public int Id { get; set; }
        public string ClientId { get; set; }
        public string RedirectUri { get; set; }
        public string PostLogoutRedirectUri { get; set; }
    }
}
