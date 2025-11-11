using System;
using Microsoft.AspNetCore.Identity;

namespace URLShorten.Data.IdentityEntities
{
    public class UrlLinksRole : IdentityRole
    {
       

        // Optional description for admin/help text
        public string? Description { get; set; }

        

        
    }
}
