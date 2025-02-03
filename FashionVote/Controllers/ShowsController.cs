using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FashionVote.Data;
using FashionVote.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace FashionVote.Controllers
{
    public class ShowsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public ShowsController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        /// <summary>
        /// Lists all upcoming shows.
        /// </summary>
        /// <returns>Returns the Index view with upcoming shows.</returns>
        /// <example>GET /Shows/Index</example>
        [AllowAnonymous]
        public async Task<IActionResult> Index()
        {
            var shows = await _context.Shows
                .Where(s => s.StartTime > DateTime.UtcNow)
                .OrderBy(s => s.StartTime)
                .ToListAsync();
            return View(shows);
        }

        /// <summary>
        /// Admin dashboard for managing shows.
        /// </summary>
        /// <returns>Returns the AdminIndex view with all shows.</returns>
        /// <example>GET /Shows/AdminIndex</example>
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AdminIndex()
        {
            var shows = await _context.Shows
                .Include(s => s.Participants)
                .Include(s => s.DesignerShows)
                .ThenInclude(ds => ds.Designer)
                .ToListAsync();
            return View(shows);
        }

        /// <summary>
        /// Displays the participant's registered shows.
        /// </summary>
        /// <returns>Returns the MyShows view with registered shows.</returns>
        /// <example>GET /Shows/MyShows</example>
        [Authorize(Roles = "Participant")]
        public async Task<IActionResult> MyShows()
        {
            var userEmail = User.Identity.Name;
            var participant = await _context.Participants
                .Include(p => p.ParticipantShows)
                .ThenInclude(ps => ps.Show)
                .FirstOrDefaultAsync(p => p.Email == userEmail);

            if (participant == null || !participant.ParticipantShows.Any())
            {
                TempData["ErrorMessage"] = "You haven't registered for any shows yet.";
                return RedirectToAction("Index");
            }

            var registeredShows = participant.ParticipantShows.Select(ps => ps.Show).ToList();
            return View(registeredShows);
        }

        /// <summary>
        /// Allows a participant to register for a show.
        /// </summary>
        /// <param name="showId">The ID of the show to register for.</param>
        /// <returns>Redirects to MyShows if successful, otherwise an error message.</returns>
        /// <example>POST /Shows/Register/5</example>
        [Authorize(Roles = "Participant")]
        public async Task<IActionResult> Register(int showId)
        {
            var userEmail = User.Identity.Name;
            var participant = await _context.Participants
                .Include(p => p.ParticipantShows)
                .ThenInclude(ps => ps.Show)
                .FirstOrDefaultAsync(p => p.Email == userEmail);

            if (participant == null)
            {
                TempData["ErrorMessage"] = "Only registered participants can register for shows.";
                return RedirectToAction(nameof(Index));
            }

            if (participant.ParticipantShows.Any(ps => ps.ShowId == showId))
            {
                TempData["ErrorMessage"] = "You are already registered for this show.";
                return RedirectToAction(nameof(MyShows));
            }

            var newShow = await _context.Shows.FindAsync(showId);
            if (newShow == null)
            {
                TempData["ErrorMessage"] = "The show you are trying to register for does not exist.";
                return RedirectToAction(nameof(Index));
            }

            if (participant.ParticipantShows.Any(ps =>
                ps.Show.StartTime < newShow.EndTime && ps.Show.EndTime > newShow.StartTime))
            {
                TempData["ErrorMessage"] = "You cannot register due to a scheduling conflict.";
                return RedirectToAction(nameof(MyShows));
            }

            participant.ParticipantShows.Add(new ParticipantShow
            {
                ParticipantId = participant.ParticipantId,
                ShowId = showId
            });

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "You have successfully registered for this show!";
            return RedirectToAction(nameof(MyShows));
        }

        /// <summary>
        /// Displays the form to create a new show.
        /// </summary>
        /// <returns>Returns the Create view.</returns>
        /// <example>GET /Shows/Create</example>
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        /// <summary>
        /// Processes the creation of a new show.
        /// </summary>
        /// <param name="show">The show details.</param>
        /// <returns>Redirects to AdminIndex on success or reloads the form on failure.</returns>
        /// <example>POST /Shows/Create</example>
        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Show show)
        {
            show.StartTime = show.StartTime.ToUniversalTime();
            show.EndTime = show.EndTime.ToUniversalTime();

            if (show.StartTime <= DateTime.UtcNow)
            {
                ModelState.AddModelError("StartTime", "Start Time must be in the future!");
            }

            if (show.StartTime >= show.EndTime)
            {
                ModelState.AddModelError("EndTime", "End Time must be after Start Time!");
            }

            if (!ModelState.IsValid)
            {
                return View(show);
            }

            _context.Add(show);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Show added successfully!";
            return RedirectToAction(nameof(AdminIndex));
        }

        /// <summary>
        /// Displays the form to edit a show.
        /// </summary>
        /// <param name="id">The ID of the show to edit.</param>
        /// <returns>Returns the Edit view with show details.</returns>
        /// <example>GET /Shows/Edit/5</example>
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id)
        {
            var show = await _context.Shows.FindAsync(id);
            if (show == null) return NotFound();

            return View(show);
        }

        /// <summary>
        /// Updates an existing show's details.
        /// </summary>
        /// <param name="id">The ID of the show to update.</param>
        /// <param name="show">Updated show details.</param>
        /// <returns>Redirects to AdminIndex on success or reloads the form on failure.</returns>
        /// <example>POST /Shows/Edit/5</example>
        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Show show)
        {
            if (id != show.ShowId) return NotFound();

            if (ModelState.IsValid)
            {
                _context.Update(show);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Show updated successfully!";
                return RedirectToAction(nameof(AdminIndex));
            }
            return View(show);
        }

        /// <summary>
        /// Displays the delete confirmation page for a show.
        /// </summary>
        /// <param name="id">The ID of the show to delete.</param>
        /// <returns>Returns the Delete view with show details.</returns>
        /// <example>GET /Shows/Delete/5</example>
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var show = await _context.Shows.FindAsync(id);
            if (show == null) return NotFound();

            return View(show);
        }

        /// <summary>
        /// Deletes a show from the system.
        /// </summary>
        /// <param name="id">The ID of the show to delete.</param>
        /// <returns>Redirects to AdminIndex after deletion.</returns>
        /// <example>POST /Shows/DeleteConfirmed/5</example>
        [HttpPost, ActionName("Delete")]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var show = await _context.Shows.FindAsync(id);
            if (show != null)
            {
                _context.Shows.Remove(show);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Show deleted successfully!";
            }
            return RedirectToAction(nameof(AdminIndex));
        }
    }
}
