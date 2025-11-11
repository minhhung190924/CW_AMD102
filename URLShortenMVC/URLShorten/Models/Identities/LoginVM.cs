using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace URLShorten.Models.Identities
{
    [Bind("Email, Password, RememberMe, ReturnUrl, ExternalLogins")]
    public class LoginVM
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;


        [Display(Name = "Làm ơn nhớ dùm nha mấy cha nội?")]
        public bool RememberMe { get; set; }


        public IList<AuthenticationScheme>? ExternalLogins { get; set; }
        public string ReturnUrl { get; set; } = "~/";

        //[TempData]
        //public string? ErrorMessage { get; set; }
    }
}