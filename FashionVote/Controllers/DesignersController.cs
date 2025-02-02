using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using FashionVote.Data;
using FashionVote.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;

namespace FashionVote.Controllers
{
    public class DesignersController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DesignersController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var designers = await _context.Designers
            .Include(d => d.DesignerShows) // Ensure related data is fetched
            .ThenInclude(ds => ds.Show) // Ensure Shows are included
            .ToListAsync();
        
            return View(designers ?? new List<Designer>());
        }

        // ✅ Display Form for Creating a Designer
        public async Task<IActionResult> Create()
        {
            ViewBag.Shows = await _context.Shows.ToListAsync(); // ✅ Fetch Available Shows
            return View();
        }

        // ✅ Process Designer Creation & Assign to Show
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Designer designer, int[] SelectedShowIds)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Shows = await _context.Shows.ToListAsync();
                TempData["ErrorMessage"] = "Validation failed. Please check the inputs.";
                return View(designer);
            }

            try
            {
                // ✅ Add Designer to DB
                _context.Add(designer);
                await _context.SaveChangesAsync();

                // ✅ Add Designer-Show relationships
                foreach (var showId in SelectedShowIds)
                {
                    _context.DesignerShows.Add(new DesignerShow
                    {
                        DesignerId = designer.DesignerId,
                        ShowId = showId
                    });
                }

                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Designer added successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Database error: " + ex.Message;
                ViewBag.Shows = await _context.Shows.ToListAsync();
                return View(designer);
            }
        }




        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var designer = await _context.Designers
                .Include(d => d.DesignerShows)
                .ThenInclude(ds => ds.Show)
                .FirstOrDefaultAsync(d => d.DesignerId == id);

            if (designer == null) return NotFound();

            // Get all available shows
            ViewBag.ShowList = new MultiSelectList(_context.Shows, "ShowId", "ShowName", 
                designer.DesignerShows.Select(ds => ds.ShowId)); // Pre-select existing shows

            return View(designer);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Designer designer, int[] SelectedShowIds)
        {
            if (id != designer.DesignerId) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(designer);
                    await _context.SaveChangesAsync();

                    // Handle Many-to-Many Relationship
                    var existingShows = _context.DesignerShows.Where(ds => ds.DesignerId == id);
                    _context.DesignerShows.RemoveRange(existingShows); // Clear previous assignments
                    
                    foreach (var showId in SelectedShowIds)
                    {
                        _context.DesignerShows.Add(new DesignerShow
                        {
                            DesignerId = id,
                            ShowId = showId
                        });
                    }

                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Designers.Any(d => d.DesignerId == id)) return NotFound();
                    throw;
                }

                TempData["SuccessMessage"] = "Designer updated successfully!";
                return RedirectToAction(nameof(Index));
            }

            ViewBag.ShowList = new MultiSelectList(_context.Shows, "ShowId", "ShowName", SelectedShowIds);
            TempData["ErrorMessage"] = "Failed to update designer. Please check inputs.";
            return View(designer);
        }


        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var designer = await _context.Designers
                .Include(d => d.DesignerShows) // Include related shows
                .ThenInclude(ds => ds.Show) // Ensure we get the Show details
                .FirstOrDefaultAsync(d => d.DesignerId == id);

            if (designer == null) return NotFound();

            return View(designer);
        }


        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var designer = await _context.Designers
                .Include(d => d.DesignerShows) // Include relationships to remove properly
                .FirstOrDefaultAsync(d => d.DesignerId == id);

            if (designer == null) return NotFound();

            // ✅ Remove related entries first
            if (designer.DesignerShows != null && designer.DesignerShows.Any())
            {
                _context.DesignerShows.RemoveRange(designer.DesignerShows);
            }

            _context.Designers.Remove(designer);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Designer deleted successfully!";
            return RedirectToAction(nameof(Index));
        }

        // ✅ ADMIN: View Designer Details
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Details(int id)
        {
            var designer = await _context.Designers
                .Include(d => d.DesignerShows)
                .ThenInclude(ds => ds.Show)
                .FirstOrDefaultAsync(d => d.DesignerId == id);

            if (designer == null)
            {
                return NotFound("Designer not found.");
            }

            return View(designer);
        }


    }
}
