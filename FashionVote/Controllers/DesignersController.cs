using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using FashionVote.Data;
using FashionVote.Models;
using Microsoft.AspNetCore.Authorization;

namespace FashionVote.Controllers
{
    public class DesignersController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DesignersController(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Displays a list of all designers along with the shows they are participating in.
        /// </summary>
        /// <returns>Returns the list of designers in the Index view.</returns>
        /// <example>GET /Designers/Index</example>
        public async Task<IActionResult> Index()
        {
            var designers = await _context.Designers
                .Include(d => d.DesignerShows)
                .ThenInclude(ds => ds.Show)
                .ToListAsync();

            return View(designers ?? new List<Designer>());
        }

        /// <summary>
        /// Displays the form to create a new designer and assign them to shows.
        /// </summary>
        /// <returns>Returns the Create view with available shows to assign.</returns>
        /// <example>GET /Designers/Create</example>
        public async Task<IActionResult> Create()
        {
            ViewBag.Shows = await _context.Shows.ToListAsync();
            return View();
        }

        /// <summary>
        /// Processes the creation of a new designer and assigns them to selected shows.
        /// </summary>
        /// <param name="designer">The designer to be created.</param>
        /// <param name="SelectedShowIds">Array of show IDs to assign the designer to.</param>
        /// <returns>Redirects to the Index view on success, or reloads the Create view on failure.</returns>
        /// <example>POST /Designers/Create</example>
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
                _context.Add(designer);
                await _context.SaveChangesAsync();

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

        /// <summary>
        /// Displays the form to edit an existing designer's details and assigned shows.
        /// </summary>
        /// <param name="id">The ID of the designer to edit.</param>
        /// <returns>Returns the Edit view with pre-filled designer details and available shows.</returns>
        /// <example>GET /Designers/Edit/5</example>
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var designer = await _context.Designers
                .Include(d => d.DesignerShows)
                .ThenInclude(ds => ds.Show)
                .FirstOrDefaultAsync(d => d.DesignerId == id);

            if (designer == null) return NotFound();

            ViewBag.ShowList = new MultiSelectList(_context.Shows, "ShowId", "ShowName", 
                designer.DesignerShows.Select(ds => ds.ShowId));

            return View(designer);
        }

        /// <summary>
        /// Processes the update of an existing designer's details and assigned shows.
        /// </summary>
        /// <param name="id">The ID of the designer to update.</param>
        /// <param name="designer">Updated designer details.</param>
        /// <param name="SelectedShowIds">Array of updated show IDs assigned to the designer.</param>
        /// <returns>Redirects to the Index view on success, or reloads the Edit view on failure.</returns>
        /// <example>POST /Designers/Edit/5</example>
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

                    var existingShows = _context.DesignerShows.Where(ds => ds.DesignerId == id);
                    _context.DesignerShows.RemoveRange(existingShows);

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

        /// <summary>
        /// Displays a confirmation page for deleting a designer.
        /// </summary>
        /// <param name="id">The ID of the designer to delete.</param>
        /// <returns>Returns the Delete view with designer details.</returns>
        /// <example>GET /Designers/Delete/5</example>
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var designer = await _context.Designers
                .Include(d => d.DesignerShows)
                .ThenInclude(ds => ds.Show)
                .FirstOrDefaultAsync(d => d.DesignerId == id);

            if (designer == null) return NotFound();

            return View(designer);
        }

        /// <summary>
        /// Deletes a designer and removes associated relationships.
        /// </summary>
        /// <param name="id">The ID of the designer to delete.</param>
        /// <returns>Redirects to the Index view on success.</returns>
        /// <example>POST /Designers/DeleteConfirmed/5</example>
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var designer = await _context.Designers
                .Include(d => d.DesignerShows)
                .FirstOrDefaultAsync(d => d.DesignerId == id);

            if (designer == null) return NotFound();

            if (designer.DesignerShows != null && designer.DesignerShows.Any())
            {
                _context.DesignerShows.RemoveRange(designer.DesignerShows);
            }

            _context.Designers.Remove(designer);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Designer deleted successfully!";
            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// Displays details of a specific designer, including their assigned shows.
        /// </summary>
        /// <param name="id">The ID of the designer to view.</param>
        /// <returns>Returns the Details view with designer and show details.</returns>
        /// <example>GET /Designers/Details/5</example>
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
