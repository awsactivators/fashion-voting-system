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

        // ✅ PUBLIC: List all upcoming shows
        [AllowAnonymous]
        public async Task<IActionResult> Index()
        {
            var shows = await _context.Shows
                .Where(s => s.StartTime > DateTime.UtcNow) // Show only upcoming events
                .ToListAsync();
            return View(shows);
        }

        // ✅ PARTICIPANT: View their registered shows
        [Authorize(Roles = "Participant")]
        public async Task<IActionResult> MyShows()
        {
            var userEmail = User.Identity.Name;
            var participant = await _context.Participants
                .Include(p => p.Show)
                .FirstOrDefaultAsync(p => p.Email == userEmail);

            if (participant == null)
            {
                TempData["ErrorMessage"] = "You haven't registered for any shows yet.";
                return RedirectToAction("Index");
            }

            var registeredShows = await _context.Shows
                .Where(s => s.ShowId == participant.ShowId)
                .ToListAsync();

            return View(registeredShows);
        }

        // ✅ PARTICIPANT: Register for a show
        [Authorize(Roles = "Participant")]
        public async Task<IActionResult> Register(int showId)
        {
            var userEmail = User.Identity.Name;
            var participant = await _context.Participants.FirstOrDefaultAsync(p => p.Email == userEmail);

            if (participant == null)
            {
                TempData["ErrorMessage"] = "Only registered participants can sign up for shows.";
                return RedirectToAction(nameof(Index));
            }

            participant.ShowId = showId;
            _context.Update(participant);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "You have successfully registered for this show!";
            return RedirectToAction(nameof(MyShows));
        }

        // ✅ PARTICIPANT: Vote for designers (Allowed only during show hours)
        [Authorize(Roles = "Participant")]
        public async Task<IActionResult> Vote(int showId)
        {
            var userEmail = User.Identity.Name;
            var participant = await _context.Participants
                .FirstOrDefaultAsync(p => p.Email == userEmail && p.ShowId == showId);

            if (participant == null)
            {
                TempData["ErrorMessage"] = "You are not registered for this show.";
                return RedirectToAction(nameof(MyShows));
            }

            var show = await _context.Shows
                .Include(s => s.DesignerShows)
                .ThenInclude(ds => ds.Designer)
                .FirstOrDefaultAsync(s => s.ShowId == showId);

            if (show == null || DateTime.UtcNow < show.StartTime || DateTime.UtcNow > show.EndTime)
            {
                TempData["ErrorMessage"] = "Voting is only available during the event.";
                return RedirectToAction(nameof(MyShows));
            }

            return View(show);
        }

        // ✅ ADMIN: Manage all shows
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

        // ✅ ADMIN: Create a new show
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public IActionResult Create()
        {
            if (!User.IsInRole("Admin"))
            {
                Console.WriteLine("❌ User is NOT an Admin!"); // Debugging
                return RedirectToAction("Index");
            }

            Console.WriteLine("✅ User is an Admin. Loading Create Show page...");
            return View();
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Show show)
        {
            if (ModelState.IsValid)
            {
                _context.Add(show);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Show added successfully!";
                return RedirectToAction(nameof(AdminIndex));
            }
            TempData["ErrorMessage"] = "Failed to add show. Please check the inputs.";
            return View(show);
        }

        // ✅ ADMIN: Edit a show
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id)
        {
            var show = await _context.Shows.FindAsync(id);
            if (show == null) return NotFound();

            return View(show);
        }

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

        // ✅ ADMIN: Delete a show
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var show = await _context.Shows.FindAsync(id);
            if (show == null) return NotFound();

            return View(show);
        }

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

        // ✅ ADMIN: View Votes for a Specific Show
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Votes(int id)
        {
            var show = await _context.Shows
                .Include(s => s.Votes)
                .ThenInclude(v => v.Participant)
                .Include(s => s.Votes)
                .ThenInclude(v => v.Designer)
                .FirstOrDefaultAsync(s => s.ShowId == id);

            if (show == null)
            {
                return NotFound("Show not found.");
            }

            return View(show);
        }

        // ✅ ADMIN & PARTICIPANT: View Show Details
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Details(int id)
        {
            var show = await _context.Shows
                .Include(s => s.DesignerShows)
                .ThenInclude(ds => ds.Designer)
                .Include(s => s.Participants)
                .FirstOrDefaultAsync(s => s.ShowId == id);

            if (show == null)
            {
                return NotFound("Show not found.");
            }

            return View(show);
        }


    }
}
