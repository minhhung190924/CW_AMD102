using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using URLShorten.Data;
using URLShorten.Data.Entities;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Authorization;

namespace URLShorten.Controllers 
{
    [Authorize]
    public class UrlLinksController : Controller
    {
        private readonly UrlShortenDbContext _context;

        public UrlLinksController(UrlShortenDbContext context)
        {
            _context = context;
        }

        // GET: UrlLinks
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Index()
        {
            var links = await _context.UrlLinks.ToListAsync();
            return View(links);
        }

        // GET: UrlLinks/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: UrlLinks/Create (AJAX compatible)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("OriginalUrl,ShortenedUrl,CustomAlias")] UrlLink urlLink)
        {
            //// Lấy user hiện tại dựa trên email đăng nhập
            //var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == User.Identity.Name);
            //if (user != null)
            //    urlLink.UserId = user.Id;

            // Ignore ShortenedUrl during model validation (we generate it server-side if missing)
            ModelState.Remove(nameof(UrlLink.ShortenedUrl));

            if (ModelState.IsValid)
            {
                if (string.IsNullOrWhiteSpace(urlLink.ShortenedUrl))
                {
                    const int attempts = 5;
                    string slug = null;
                    for (int i = 0; i < attempts; i++)
                    {
                        var candidate = GenerateRandomSlug(6);
                        var candidateFull = $"{Request.Scheme}://{Request.Host}/r/{candidate}";
                        if (!await _context.UrlLinks.AnyAsync(x => x.ShortenedUrl == candidateFull))
                        {
                            slug = candidate;
                            urlLink.ShortenedUrl = candidateFull;
                            break;
                        }
                    }
                    if (slug == null)
                        return StatusCode(500, "Unable to generate unique shortened URL. Try again.");
                }

                urlLink.CreatedDate = DateTime.UtcNow;
                urlLink.ClickCount = 0;
                urlLink.IsActive = true;

                _context.Add(urlLink);
                await _context.SaveChangesAsync();
                return Ok();
            }

            return BadRequest("Invalid data");
            //if (ModelState.IsValid)
            //{
            //    // Prevent duplicate slug/full shortened URL
            //    if (await _context.UrlLinks.AnyAsync(x => x.ShortenedUrl == urlLink.ShortenedUrl))
            //    {
            //        return BadRequest("Shortened URL already exists. Try again.");
            //    }

            //    urlLink.CreatedDate = DateTime.UtcNow;
            //    urlLink.ClickCount = 0;
            //    urlLink.IsActive = true;

            //    _context.Add(urlLink);
            //    await _context.SaveChangesAsync();
            //    return Ok(); // AJAX success
            //}

            //return BadRequest("Invalid data");
        }

        // GET: UrlLinks/ReloadList (used by AJAX after create)
        [HttpGet]
        public IActionResult ReloadList()
        {
            return ViewComponent("UrlLinksList");
        }

        // Generate short random code
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GenerateShort([FromForm] string originalUrl)
        {
            if (string.IsNullOrWhiteSpace(originalUrl))
                return BadRequest("Original URL is required.");

            // Generate random slug (6 characters) on a background thread
            string newSlug = await Task.Run(() => GenerateRandomSlug(6));

            // Build the full shortened URL using the /r/ pattern
            string shortenedUrl = $"{Request.Scheme}://{Request.Host}/r/{newSlug}";

            // Return both slug and shortened URL for your JS
            return Json(new { slug = newSlug, shortenedUrl });
        }

        // Redirect to original URL
        [HttpGet("/r/{slug}")]
        public async Task<IActionResult> RedirectToOriginal(string slug)
        {
            if (string.IsNullOrWhiteSpace(slug))
                return NotFound();

            var urlLink = await _context.UrlLinks
                .FirstOrDefaultAsync(u => u.ShortenedUrl.EndsWith("/r/" + slug) && u.IsActive);

            if (urlLink == null)
                return NotFound();

            urlLink.ClickCount++;
            _context.Update(urlLink);
            await _context.SaveChangesAsync();

            return Redirect(urlLink.OriginalUrl);
        }


        // GET: UrlLinks/Edit/5
        [Authorize(Roles = "Admin")]

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var urlLink = await _context.UrlLinks.FindAsync(id);
            if (urlLink == null) return NotFound();

            return View(urlLink);
        }

        // POST: UrlLinks/Edit/5
        [Authorize(Roles = "Admin")]

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,OriginalUrl,ShortenedUrl,CustomAlias,ClickCount,IsActive,CreatedDate,UserId")] UrlLink urlLink)
        {
            if (id != urlLink.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(urlLink);
                    await _context.SaveChangesAsync();

                    // keep user on the Edit page after save
                    ViewData["SuccessMessage"] = "Saved successfully.";
                    return View(urlLink);
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!UrlLinkExists(urlLink.Id))
                        return NotFound();
                    else
                        throw;
                }
            }

            return View(urlLink);
        }

        // GET: UrlLinks/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var urlLink = await _context.UrlLinks
                .FirstOrDefaultAsync(m => m.Id == id);
            if (urlLink == null) return NotFound();

            return PartialView(urlLink);
        }

        // POST: UrlLinks/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var urlLink = await _context.UrlLinks.FindAsync(id);
            if (urlLink != null)
            {
                _context.UrlLinks.Remove(urlLink);
                await _context.SaveChangesAsync();
            }

            // ====== If AJAX call, return JSON so client JS can handle without redirect 
            var isAjax = Request.Headers["X-Requested-With"] == "XMLHttpRequest"
                         || Request.Headers["Accept"].ToString().Contains("application/json");
            if (isAjax)
            {
                // Kiểm tra còn link nào không
                var anyLinks = await _context.UrlLinks.AnyAsync();
                if (!anyLinks)
                {
                    // Trả về JSON báo hiệu cần chuyển hướng
                    return Json(new { isOK = true, redirectToCreate = true });
                }

                // Trả về JSON bình thường
                return Json(new { isOK = true });
            }

            return RedirectToAction(nameof(Index));
        }

        // Utility: Generate random short slug
        private string GenerateRandomSlug(int length)
        {
            const string chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var bytes = new byte[length];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(bytes);
            }
            var result = new StringBuilder(length);
            foreach (var b in bytes)
                result.Append(chars[b % chars.Length]);
            return result.ToString();
        }

        private bool UrlLinkExists(int id)
        {
            return _context.UrlLinks.Any(e => e.Id == id);
        }

        // GET: UrlLinks/Details/5
        //public async Task<IActionResult> Details(int id)
        //{
        //    //var link = await _context.UrlLinks
        //    //    .Include(u => u.User)
        //    //    .FirstOrDefaultAsync(u => u.Id == id);
        //    //if (link == null) return NotFound();

        //    // Lấy email nếu có user
        //    //var userEmail = link.User?.Email ?? "(No user)";

        //    // Trả về HTML chi tiết (không tạo file mới, render trực tiếp)
        //    return Content($@"
        //        <div>
        //            <strong>Original URL:</strong> <a href='{link.OriginalUrl}' target='_blank'>{link.OriginalUrl}</a><br/>
        //            <strong>Shortened URL:</strong> <a href='{link.ShortenedUrl}' target='_blank'>{link.ShortenedUrl}</a><br/>
        //            <strong>Created Date:</strong> {link.CreatedDate}<br/>
        //            <strong>Click Count:</strong> {link.ClickCount}<br/>
        //            <strong>Created By (Email):</strong> {userEmail}
        //        </div>
        //    ", "text/html");
        //}
    }
}
