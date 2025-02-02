using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
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
        /// Displays a list of all participants with their shows.
        /// </summary>
        public async Task<IActionResult> Index()
        {
            var participants = await _context.Participants
                .Include(p => p.Show)  // Include Show details
                .Include(p => p.Votes) // Include votes
                .ToListAsync();
            return View(participants);
        }

        /// <summary>
        /// Displays the form to create a new participant.
        /// </summary>
        // ✅ GET: Participants/Create (Show form with Show dropdown)
        public IActionResult Create()
        {
            var shows = _context.Shows.ToList();
            if (shows == null || !shows.Any())
            {
                TempData["ErrorMessage"] = "No available shows. Please create a show first.";
                return RedirectToAction("Index");
            }

            ViewBag.Shows = new SelectList(shows, "ShowId", "ShowName");
            return View();
        }



        // ✅ POST: Participants/Create (Save participant with selected Show)
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
                
                // Reload the Show dropdown to prevent errors
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
        // [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id)
        {
            var participant = await _context.Participants.FindAsync(id);
            if (participant == null) return NotFound();

            ViewBag.Shows = new SelectList(_context.Shows, "ShowId", "ShowName", participant.ShowId);
            return View(participant);
        }


        /// <summary>
        /// Updates an existing participant.
        /// </summary>
        [HttpPost]
        // [Authorize(Roles = "Adn")]mi
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
        /// Displays the delete confirmation page.
        /// </summary>
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var participant = await _context.Participants.Include(p => p.Show).FirstOrDefaultAsync(m => m.ParticipantId == id);
            if (participant == null) return NotFound();

            return View(participant);
        }

        /// <summary>
        /// Deletes a participant.
        /// </summary>
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
