using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using URLShorten.Data;
using System.Threading.Tasks;
using System.Linq;

namespace URLShorten.ViewComponents
{
    public class UrlLinksList : ViewComponent
    {
        private readonly UrlShortenDbContext _context;

        public UrlLinksList(UrlShortenDbContext context)
        {
            _context = context;
        }

        public async Task<IViewComponentResult> InvokeAsync(bool isEditPage = false)
        {
            var links = await _context.UrlLinks.ToListAsync();
            ViewBag.IsEditPage = isEditPage;
            return View(links);
        }
    }
}
