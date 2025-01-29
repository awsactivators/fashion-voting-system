using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FashionVote.Data;
using FashionVote.Models;

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
        /// Displays a list of all participants.
        /// </summary>
        /// <returns>An <see cref="IActionResult"/> that renders the index view with a list of participants.</returns>
        /// <example>
        /// GET: /Participants
        /// </example>
        public async Task<IActionResult> Index()
        {
            var participants = await _context.Participants.ToListAsync();
            return View(participants);
        }

        /// <summary>
        /// Displays the form to create a new participant.
        /// </summary>
        /// <returns>An <see cref="IActionResult"/> that renders the create view.</returns>
        /// <example>
        /// GET: /Participants/Create
        /// </example>
        public IActionResult Create()
        {
            return View();
        }

        /// <summary>
        /// Processes the creation of a new participant.
        /// </summary>
        /// <param name="participant">The <see cref="Participant"/> object to be created.</param>
        /// <returns>
        /// Redirects to the index view on success.
        /// Returns the create view with validation errors if the model state is invalid.
        /// </returns>
        /// <example>
        /// POST: /Participants/Create
        /// Body: { "Name": "John Doe", "Email": "john@example.com" }
        /// </example>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Participant participant)
        {
            if (ModelState.IsValid)
            {
                _context.Add(participant);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Participant added successfully!";
                return RedirectToAction(nameof(Index));
            }
            TempData["ErrorMessage"] = "Failed to add participant. Please check the inputs.";
            return View(participant);
        }

        /// <summary>
        /// Displays the form to edit an existing participant.
        /// </summary>
        /// <param name="id">The ID of the participant to edit.</param>
        /// <returns>
        /// An <see cref="IActionResult"/> that renders the edit view.
        /// Returns a 404 error if the participant is not found.
        /// </returns>
        /// <example>
        /// GET: /Participants/Edit/1
        /// </example>
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var participant = await _context.Participants.FindAsync(id);
            if (participant == null) return NotFound();

            return View(participant);
        }

        /// <summary>
        /// Processes the update of an existing participant.
        /// </summary>
        /// <param name="id">The ID of the participant being updated.</param>
        /// <param name="participant">The updated participant data.</param>
        /// <returns>
        /// Redirects to the index view on success.
        /// Returns the edit view with validation errors if the model state is invalid.
        /// </returns>
        /// <example>
        /// POST: /Participants/Edit/1
        /// Body: { "ParticipantId": 1, "Name": "Jane Doe", "Email": "jane@example.com" }
        /// </example>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Participant participant)
        {
            if (id != participant.ParticipantId) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(participant);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Participants.Any(p => p.ParticipantId == id)) return NotFound();
                    throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(participant);
        }

        /// <summary>
        /// Displays the confirmation page to delete a participant.
        /// </summary>
        /// <param name="id">The ID of the participant to delete.</param>
        /// <returns>
        /// An <see cref="IActionResult"/> that renders the delete view.
        /// Returns a 404 error if the participant is not found.
        /// </returns>
        /// <example>
        /// GET: /Participants/Delete/1
        /// </example>
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var participant = await _context.Participants.FindAsync(id);
            if (participant == null) return NotFound();

            return View(participant);
        }

        /// <summary>
        /// Processes the deletion of a participant.
        /// </summary>
        /// <param name="id">The ID of the participant to delete.</param>
        /// <returns>
        /// Redirects to the index view on success.
        /// </returns>
        /// <example>
        /// POST: /Participants/Delete/1
        /// </example>
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var participant = await _context.Participants.FindAsync(id);
            _context.Participants.Remove(participant);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
    }
}
