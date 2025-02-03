using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using FashionVote.Data;
using FashionVote.Models;
using Microsoft.AspNetCore.Authorization;

namespace FashionVote.Controllers
{
    public class ParticipantsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ParticipantsController(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Displays a list of all participants along with the shows they are registered for.
        /// </summary>
        /// <returns>Returns the Index view with a list of participants.</returns>
        /// <example>GET /Participants/Index</example>
        public async Task<IActionResult> Index()
        {
            var participants = await _context.Participants
                .Include(p => p.Show)
                .Include(p => p.Votes)
                .ToListAsync();

            return View(participants);
        }

        /// <summary>
        /// Redirects to the participant index page with an error message.
        /// Participants must register themselves.
        /// </summary>
        /// <returns>Redirects to Index with an error message.</returns>
        /// <example>GET /Participants/Create</example>
        [Authorize(Roles = "Admin")]
        public IActionResult Create()
        {
            TempData["ErrorMessage"] = "Participants must register themselves.";
            return RedirectToAction("Index");
        }

        /// <summary>
        /// Processes the creation of a participant.
        /// </summary>
        /// <param name="participant">The participant to be created.</param>
        /// <returns>Redirects to Index on success or reloads the form on failure.</returns>
        /// <example>POST /Participants/Create</example>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Participant participant)
        {
            Console.WriteLine("Received ShowId: " + participant.ShowId);
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors)
                                            .Select(e => e.ErrorMessage)
                                            .ToList();

                TempData["ErrorMessage"] = "Validation failed: " + string.Join(", ", errors);
                
                ViewBag.Shows = new SelectList(await _context.Shows.ToListAsync(), "ShowId", "ShowName", participant.ShowId);
                return View(participant);
            }

            try
            {
                _context.Add(participant);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Participant added successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error saving participant: " + ex.Message;
                ViewBag.Shows = new SelectList(await _context.Shows.ToListAsync(), "ShowId", "ShowName", participant.ShowId);
                return View(participant);
            }
        }

        /// <summary>
        /// Displays the form to edit a participant.
        /// </summary>
        /// <param name="id">The ID of the participant to edit.</param>
        /// <returns>Returns the Edit view with pre-filled participant details.</returns>
        /// <example>GET /Participants/Edit/5</example>
        public async Task<IActionResult> Edit(int id)
        {
            var participant = await _context.Participants.FindAsync(id);
            if (participant == null) return NotFound();

            ViewBag.Shows = new SelectList(_context.Shows, "ShowId", "ShowName", participant.ShowId);
            return View(participant);
        }

        /// <summary>
        /// Updates an existing participant's details.
        /// </summary>
        /// <param name="id">The ID of the participant to update.</param>
        /// <param name="participant">Updated participant details.</param>
        /// <returns>Redirects to Index on success or reloads the form on failure.</returns>
        /// <example>POST /Participants/Edit/5</example>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Participant participant)
        {
            if (id != participant.ParticipantId) return NotFound();

            if (ModelState.IsValid)
            {
                _context.Update(participant);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Participant updated successfully!";
                return RedirectToAction(nameof(Index));
            }

            ViewBag.Shows = new SelectList(_context.Shows, "ShowId", "ShowName", participant.ShowId);
            return View(participant);
        }

        /// <summary>
        /// Displays the delete confirmation page for a participant.
        /// </summary>
        /// <param name="id">The ID of the participant to delete.</param>
        /// <returns>Returns the Delete view with participant details.</returns>
        /// <example>GET /Participants/Delete/5</example>
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var participant = await _context.Participants
                .Include(p => p.Show)
                .FirstOrDefaultAsync(m => m.ParticipantId == id);

            if (participant == null) return NotFound();

            return View(participant);
        }

        /// <summary>
        /// Deletes a participant from the system.
        /// </summary>
        /// <param name="id">The ID of the participant to delete.</param>
        /// <returns>Redirects to Index after deletion.</returns>
        /// <example>POST /Participants/DeleteConfirmed/5</example>
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var participant = await _context.Participants.FindAsync(id);
            _context.Participants.Remove(participant);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Participant deleted successfully!";
            return RedirectToAction(nameof(Index));
        }
    }
}
