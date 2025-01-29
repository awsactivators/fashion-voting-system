using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FashionVote.Data;
using FashionVote.Models;

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
        /// Retrieves a list of designers from the database.
        /// </summary>
        /// <returns>
        /// - A list of <see cref="Designer"/> objects with an HTTP 200 status code if found.
        /// - An empty list if no designers exist in the database.
        /// </returns>
        /// <example>
        /// GET: api/designers
        /// 
        /// Response:
        /// [
        ///     {"designerId": 1, "name": "John Doe", "category": "Haute Couture", "createdAt": "2025-01-01T00:00:00"},
        ///     {"designerId": 2, "name": "Jane Smith", "category": "Casual Wear", "createdAt": "2025-01-02T00:00:00"}
        /// ]
        /// </example>

        public async Task<IActionResult> Index()
        {
            var designers = await _context.Designers.ToListAsync();
            return View(designers);
        }

        public IActionResult Create()
        {
            return View();
        }


        /// <summary>
        /// Adds a new designer to the database.
        /// </summary>
        /// <param name="designer">The designer object to add.</param>
        /// <returns>
        /// - The created <see cref="Designer"/> object with an HTTP 201 status code.
        /// - An HTTP 400 status code if the provided designer object is invalid.
        /// </returns>
        /// <example>
        /// POST: api/designers
        /// 
        /// Request Body:
        /// {"name": "John Doe", "category": "Haute Couture"}
        /// 
        /// Response:
        /// {"designerId": 1, "name": "John Doe", "category": "Haute Couture", "createdAt": "2025-01-01T00:00:00"}
        /// </example>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Designer designer)
        {
            if (ModelState.IsValid)
            {
                _context.Add(designer);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Designer added successfully!";
                return RedirectToAction(nameof(Index));
            }
            TempData["ErrorMessage"] = "Failed to add designer. Please check the inputs.";
            return View(designer);
        }



        
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var designer = await _context.Designers.FindAsync(id);
            if (designer == null) return NotFound();

            return View(designer);
        }


        /// <summary>
        /// Updates an existing designer's details.
        /// </summary>
        /// <param name="id">The ID of the designer to update.</param>
        /// <param name="designer">The updated designer object.</param>
        /// <returns>
        /// - An HTTP 204 status code if the update is successful.
        /// - An HTTP 400 status code if the provided data is invalid.
        /// - An HTTP 404 status code if the designer with the given ID does not exist.
        /// </returns>
        /// <example>
        /// PUT: api/designers/1
        /// 
        /// Request Body:
        /// {"designerId": 1, "name": "John Doe", "category": "Casual Wear"}
        /// 
        /// Response: No Content (HTTP 204)
        /// </example>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Designer designer)
        {
            if (id != designer.DesignerId) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(designer);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Designers.Any(d => d.DesignerId == id)) return NotFound();
                    throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(designer);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var designer = await _context.Designers.FindAsync(id);
            if (designer == null) return NotFound();

            return View(designer);
        }


        /// <summary>
        /// Deletes a designer by ID.
        /// </summary>
        /// <param name="id">The ID of the designer to delete.</param>
        /// <returns>
        /// - An HTTP 204 status code if the deletion is successful.
        /// - An HTTP 404 status code if the designer is not found.
        /// </returns>
        /// <example>
        /// DELETE: api/designers/1
        /// Output: HTTP 204 No Content
        /// </example>
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var designer = await _context.Designers.FindAsync(id);
            _context.Designers.Remove(designer);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
    }
}
