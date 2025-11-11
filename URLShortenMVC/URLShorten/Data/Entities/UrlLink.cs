using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using URLShorten.Commons;

namespace URLShorten.Data.Entities
{
    [Index(nameof(ShortenedUrl), IsUnique = true)]
    public class UrlLink
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Original URL is required")]
        //[Url(ErrorMessage = "Please enter a valid URL")]
        [MaxLength(MaxLengths.OriginalURL)]
        [Display(Name = "Original URL")]
        public string OriginalUrl { get; set; } = string.Empty;

        [Required]
        //[DataType(DataType.Url)]
        //[MaxLength(MaxLengths.ShortenedURL)]
        [Display(Name = "Shortened URL")]
        public string ShortenedUrl { get; set; } = string.Empty;

        [Display(Name = "Custom Alias")]
        [MaxLength(MaxLengths.CustomAlias)]
        public string? CustomAlias { get; set; }

        [Display(Name = "Created Date")]
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        [Display(Name = "Click Count")]
        public int ClickCount { get; set; } = 0;

        [Display(Name = "Is Active")]
        public bool IsActive { get; set; } = true;

        //[Display(Name = "Created By User")]
        //public int? UserId { get; set; }

        //[ForeignKey(nameof(UserId))]
        //public virtual User? User { get; set; }
    }
}