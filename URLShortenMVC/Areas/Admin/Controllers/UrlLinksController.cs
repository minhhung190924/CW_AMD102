using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using URLShorten.Data;
using URLShorten.Data.Entities;

namespace URLShorten.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class UrlLinksController : Controller
    {
        private readonly UrlShortenDbContext _context;
        public UrlLinksController(UrlShortenDbContext context) => _context = context;

        // GET: /Admin/UrlLinks
        public async Task<IActionResult> Index(string q = null)
        {
            var query = _context.UrlLinks.AsQueryable();
            if (!string.IsNullOrWhiteSpace(q))
                query = query.Where(x => x.OriginalUrl.Contains(q) || x.ShortenedUrl.Contains(q));

            var list = await query.OrderByDescending(x => x.CreatedDate).ToListAsync();
            return View(list);
        }

        // GET: /Admin/UrlLinks/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var item = await _context.UrlLinks.FindAsync(id);
            return item == null ? NotFound() : View(item);
        }

        // POST: /Admin/UrlLinks/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,OriginalUrl,ShortenedUrl,CustomAlias,IsActive,UserId")] UrlLink model)
        {
            if (id != model.Id) return NotFound();
            if (!ModelState.IsValid) return View(model);

            var existing = await _context.UrlLinks.FindAsync(id);
            if (existing == null) return NotFound();

            // If OriginalUrl changed, optionally regenerate shortened link
            if (!string.Equals(existing.OriginalUrl?.Trim(), model.OriginalUrl?.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                // generate a new unique slug (reuse your GenerateRandomSlug method or logic)
                string candidateFull;
                do
                {
                    var slug = GenerateRandomSlug(6);
                    candidateFull = $"{Request.Scheme}://{Request.Host}/r/{slug}";
                } while (await _context.UrlLinks.AnyAsync(x => x.ShortenedUrl == candidateFull && x.Id != id));

                existing.ShortenedUrl = candidateFull;
            }

            existing.OriginalUrl = model.OriginalUrl?.Trim();
            existing.CustomAlias = model.CustomAlias;
            existing.IsActive = model.IsActive;
            existing.UserId = model.UserId;

            _context.Update(existing);
            await _context.SaveChangesAsync();

            ViewData["SuccessMessage"] = "Saved";
            return View(existing); // stay on edit
        }

        // POST: /Admin/UrlLinks/Delete/5 (AJAX-friendly)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var entity = await _context.UrlLinks.FindAsync(id);
            if (entity != null)
            {
                _context.UrlLinks.Remove(entity);
                await _context.SaveChangesAsync();
            }

            return Json(new { ok = true });
        }

        // helper (copy from your controller)
        private string GenerateRandomSlug(int length)
        {
            const string chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var bytes = new byte[length];
            using var rng = System.Security.Cryptography.RandomNumberGenerator.Create();
            rng.GetBytes(bytes);
            var sb = new System.Text.StringBuilder(length);
            foreach (var b in bytes) sb.Append(chars[b % chars.Length]);
            return sb.ToString();
        }
    }
}