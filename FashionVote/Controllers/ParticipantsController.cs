// using Microsoft.AspNetCore.Mvc;
// using Microsoft.EntityFrameworkCore;
// using FashionVote.Data;
// using FashionVote.Models;

// namespace FashionVote.Controllers
// {
//     public class ParticipantsController : Controller
//     {
//         private readonly ApplicationDbContext _context;

//         public ParticipantsController(ApplicationDbContext context)
//         {
//             _context = context;
//         }

//         /// <summary>
//         /// Displays a list of all participants.
//         /// </summary>
//         /// <returns>An <see cref="IActionResult"/> that renders the index view with a list of participants.</returns>
//         /// <example>
//         /// GET: /Participants
//         /// </example>
//         public async Task<IActionResult> Index()
//         {
//             var participants = await _context.Participants.ToListAsync();
//             return View(participants);
//         }

//         /// <summary>
//         /// Displays the form to create a new participant.
//         /// </summary>
//         /// <returns>An <see cref="IActionResult"/> that renders the create view.</returns>
//         /// <example>
//         /// GET: /Participants/Create
//         /// </example>
//         public IActionResult Create()
//         {
//             return View();
//         }

//         /// <summary>
//         /// Processes the creation of a new participant.
//         /// </summary>
//         /// <param name="participant">The <see cref="Participant"/> object to be created.</param>
//         /// <returns>
//         /// Redirects to the index view on success.
//         /// Returns the create view with validation errors if the model state is invalid.
//         /// </returns>
//         /// <example>
//         /// POST: /Participants/Create
//         /// Body: { "Name": "John Doe", "Email": "john@example.com" }
//         /// </example>
//         [HttpPost]
//         [ValidateAntiForgeryToken]
//         public async Task<IActionResult> Create(Participant participant)
//         {
//             if (ModelState.IsValid)
//             {
//                 _context.Add(participant);
//                 await _context.SaveChangesAsync();
//                 TempData["SuccessMessage"] = "Participant added successfully!";
//                 return RedirectToAction(nameof(Index));
//             }
//             TempData["ErrorMessage"] = "Failed to add participant. Please check the inputs.";
//             return View(participant);
//         }

//         /// <summary>
//         /// Displays the form to edit an existing participant.
//         /// </summary>
//         /// <param name="id">The ID of the participant to edit.</param>
//         /// <returns>
//         /// An <see cref="IActionResult"/> that renders the edit view.
//         /// Returns a 404 error if the participant is not found.
//         /// </returns>
//         /// <example>
//         /// GET: /Participants/Edit/1
//         /// </example>
//         public async Task<IActionResult> Edit(int? id)
//         {
//             if (id == null) return NotFound();

//             var participant = await _context.Participants.FindAsync(id);
//             if (participant == null) return NotFound();

//             return View(participant);
//         }

//         /// <summary>
//         /// Processes the update of an existing participant.
//         /// </summary>
//         /// <param name="id">The ID of the participant being updated.</param>
//         /// <param name="participant">The updated participant data.</param>
//         /// <returns>
//         /// Redirects to the index view on success.
//         /// Returns the edit view with validation errors if the model state is invalid.
//         /// </returns>
//         /// <example>
//         /// POST: /Participants/Edit/1
//         /// Body: { "ParticipantId": 1, "Name": "Jane Doe", "Email": "jane@example.com" }
//         /// </example>
//         [HttpPost]
//         [ValidateAntiForgeryToken]
//         public async Task<IActionResult> Edit(int id, Participant participant)
//         {
//             if (id != participant.ParticipantId) return NotFound();

//             if (ModelState.IsValid)
//             {
//                 try
//                 {
//                     _context.Update(participant);
//                     await _context.SaveChangesAsync();
//                 }
//                 catch (DbUpdateConcurrencyException)
//                 {
//                     if (!_context.Participants.Any(p => p.ParticipantId == id)) return NotFound();
//                     throw;
//                 }
//                 return RedirectToAction(nameof(Index));
//             }
//             return View(participant);
//         }

//         /// <summary>
//         /// Displays the confirmation page to delete a participant.
//         /// </summary>
//         /// <param name="id">The ID of the participant to delete.</param>
//         /// <returns>
//         /// An <see cref="IActionResult"/> that renders the delete view.
//         /// Returns a 404 error if the participant is not found.
//         /// </returns>
//         /// <example>
//         /// GET: /Participants/Delete/1
//         /// </example>
//         public async Task<IActionResult> Delete(int? id)
//         {
//             if (id == null) return NotFound();

//             var participant = await _context.Participants.FindAsync(id);
//             if (participant == null) return NotFound();

//             return View(participant);
//         }

//         /// <summary>
//         /// Processes the deletion of a participant.
//         /// </summary>
//         /// <param name="id">The ID of the participant to delete.</param>
//         /// <returns>
//         /// Redirects to the index view on success.
//         /// </returns>
//         /// <example>
//         /// POST: /Participants/Delete/1
//         /// </example>
//         [HttpPost, ActionName("Delete")]
//         [ValidateAntiForgeryToken]
//         public async Task<IActionResult> DeleteConfirmed(int id)
//         {
//             var participant = await _context.Participants.FindAsync(id);
//             _context.Participants.Remove(participant);
//             await _context.SaveChangesAsync();
//             return RedirectToAction(nameof(Index));
//         }
//     }
// }


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
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var participant = await _context.Participants.FindAsync(id);
            if (participant == null) return NotFound();

            ViewData["ShowId"] = new SelectList(_context.Shows, "ShowId", "ShowName", participant.ShowId);
            return View(participant);
        }

        /// <summary>
        /// Updates an existing participant.
        /// </summary>
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

            ViewData["ShowId"] = new SelectList(_context.Shows, "ShowId", "ShowName", participant.ShowId);
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
