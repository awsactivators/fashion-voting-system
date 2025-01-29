using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FashionVote.Data;
using FashionVote.Models;

namespace FashionVote.Controllers
{
    public class ShowsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ShowsController(ApplicationDbContext context)
        {
            _context = context;
        }


        /// <summary>
        /// Retrieves all shows from the database.
        /// </summary>
        /// <returns>
        /// - A list of <see cref="Show"/> objects with an HTTP 200 status code.
        /// </returns>
        /// <example>
        /// GET: api/shows
        /// Output: [{"showId":1,"showName":"Spring Gala","location":"New York","startTime":"2023-04-01T10:00:00","endTime":"2023-04-01T14:00:00"}]
        /// </example>
        public async Task<IActionResult> Index()
        {
            var shows = await _context.Shows.ToListAsync();
            return View(shows);
        }

        public IActionResult Create()
        {
            return View();
        }


        /// <summary>
        /// Adds a new show to the database.
        /// </summary>
        /// <param name="show">The <see cref="Show"/> object to add.</param>
        /// <returns>
        /// - The created <see cref="Show"/> object with an HTTP 201 status code.
        /// - An HTTP 400 status code if the input data is invalid.
        /// </returns>
        /// <example>
        /// POST: api/shows
        /// Body: {"showName":"Spring Gala","location":"New York","startTime":"2023-04-01T10:00:00","endTime":"2023-04-01T14:00:00"}
        /// Output: {"showId":1,"showName":"Spring Gala","location":"New York","startTime":"2023-04-01T10:00:00","endTime":"2023-04-01T14:00:00"}
        /// </example>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Show show)
        {
            if (ModelState.IsValid)
            {
                _context.Add(show);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Show added successfully!";
                return RedirectToAction(nameof(Index));
            }
            TempData["ErrorMessage"] = "Failed to add show. Please check the inputs.";
            return View(show);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var show = await _context.Shows.FindAsync(id);
            if (show == null) return NotFound();

            return View(show);
        }



        /// <summary>
        /// Updates an existing show by ID.
        /// </summary>
        /// <param name="id">The ID of the show to update.</param>
        /// <param name="show">The updated <see cref="Show"/> object.</param>
        /// <returns>
        /// - An HTTP 204 status code if the update is successful.
        /// - An HTTP 404 status code if the show is not found.
        /// - An HTTP 400 status code if the input data is invalid.
        /// </returns>
        /// <example>
        /// PUT: api/shows/1
        /// Body: {"showId":1,"showName":"Summer Extravaganza","location":"Paris","startTime":"2023-06-01T16:00:00","endTime":"2023-06-01T20:00:00"}
        /// Output: HTTP 204 No Content
        /// </example>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Show show)
        {
            if (id != show.ShowId) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(show);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Shows.Any(s => s.ShowId == id)) return NotFound();
                    throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(show);
        }




        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var show = await _context.Shows.FindAsync(id);
            if (show == null) return NotFound();

            return View(show);
        }



        /// <summary>
        /// Deletes a show by ID.
        /// </summary>
        /// <param name="id">The ID of the show to delete.</param>
        /// <returns>
        /// - An HTTP 204 status code if the deletion is successful.
        /// - An HTTP 404 status code if the show is not found.
        /// </returns>
        /// <example>
        /// DELETE: api/shows/1
        /// Output: HTTP 204 No Content
        /// </example>
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var show = await _context.Shows.FindAsync(id);
            _context.Shows.Remove(show);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
    }
}
