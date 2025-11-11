using Microsoft.AspNetCore.Identity;

namespace URLShorten.Data.IdentityEntities
{
    public class UrlLinksUser : IdentityUser    
    {
        public string? FullName { get; set; }
        public string? Avatar { get; set; }

    }
}
