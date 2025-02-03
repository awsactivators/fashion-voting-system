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
                .OrderBy(s => s.StartTime)
                .ToListAsync();

            Console.WriteLine($"✅ Fetching {shows.Count} upcoming shows from DB.");
            foreach (var show in shows)
            {
                Console.WriteLine($"Show: {show.ShowName}, Start: {show.StartTime}");
            }
            return View(shows);
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

        

        // ✅ PARTICIPANT: View their registered shows
        [Authorize(Roles = "Participant")]
        public async Task<IActionResult> MyShows()
        {
            var userEmail = User.Identity.Name;

            // ✅ Get the participant along with their registered shows
            var participant = await _context.Participants
                .Include(p => p.ParticipantShows) // Include many-to-many relationship
                .ThenInclude(ps => ps.Show) // Include the show details
                .FirstOrDefaultAsync(p => p.Email == userEmail);

            if (participant == null || !participant.ParticipantShows.Any())
            {
                TempData["ErrorMessage"] = "You haven't registered for any shows yet.";
                return RedirectToAction("Index");
            }

            // ✅ Extract registered shows
            var registeredShows = participant.ParticipantShows.Select(ps => ps.Show).ToList();

            return View(registeredShows);
        }




        // ✅ PARTICIPANT: Register for a show
        [Authorize(Roles = "Participant")]
        public async Task<IActionResult> Register(int showId)
        {
            var userEmail = User.Identity.Name;
            var participant = await _context.Participants
                .Include(p => p.ParticipantShows) // Ensure ParticipantShows is loaded
                .ThenInclude(ps => ps.Show) // Ensure Show details are included
                .FirstOrDefaultAsync(p => p.Email == userEmail);

            if (participant == null)
            {
                TempData["ErrorMessage"] = "Only registered participants can register for shows.";
                return RedirectToAction(nameof(Index));
            }

            // ✅ Check if already registered for this show
            if (participant.ParticipantShows != null &&
                participant.ParticipantShows.Any(ps => ps.ShowId == showId))
            {
                TempData["ErrorMessage"] = "You are already registered for this show.";
                return RedirectToAction(nameof(MyShows));
            }

            // ✅ Check if the show exists
            var newShow = await _context.Shows.FindAsync(showId);
            if (newShow == null)
            {
                TempData["ErrorMessage"] = "The show you are trying to register for does not exist.";
                return RedirectToAction(nameof(Index));
            }

            // ✅ Check if there is a scheduling conflict
            if (participant.ParticipantShows != null &&
                participant.ParticipantShows.Any(ps =>
                    ps.Show.StartTime < newShow.EndTime && ps.Show.EndTime > newShow.StartTime))
            {
                TempData["ErrorMessage"] = "You cannot register due to a scheduling conflict with another show.";
                return RedirectToAction(nameof(MyShows));
            }

            // ✅ Register the participant for the show
            participant.ParticipantShows ??= new List<ParticipantShow>(); // Initialize if null
            participant.ParticipantShows.Add(new ParticipantShow
            {
                ParticipantId = participant.ParticipantId,
                ShowId = showId
            });

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "You have successfully registered for this show!";
            return RedirectToAction(nameof(MyShows));
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
            // Convert input StartTime to UTC if necessary
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



        // ✅ PARTICIPANT: Vote for designers (Allowed only during show hours)
        [Authorize(Roles = "Participant")]
        public async Task<IActionResult> Vote(int showId)
        {
            var userEmail = User.Identity.Name;
            var participant = await _context.Participants
                .Include(p => p.ParticipantShows)
                .ThenInclude(ps => ps.Show)
                .FirstOrDefaultAsync(p => p.Email == userEmail);

            if (participant == null || !participant.ParticipantShows.Any(ps => ps.ShowId == showId))
            {
                TempData["ErrorMessage"] = "You are not registered for this show.";
                return RedirectToAction("MyShows");
            }

            var show = await _context.Shows
                .Include(s => s.DesignerShows)
                .ThenInclude(ds => ds.Designer)
                .FirstOrDefaultAsync(s => s.ShowId == showId);

            if (show == null)
            {
                return NotFound("Show not found.");
            }

            return View(show); // This should point to your "Vote" page
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



        [HttpPost]
        [Authorize(Roles = "Participant")]
        public async Task<IActionResult> SubmitVote(int showId, int[] designerIds)
        {
            var userEmail = User.Identity.Name;
            var participant = await _context.Participants
                .Include(p => p.ParticipantShows)
                .FirstOrDefaultAsync(p => p.Email == userEmail);

            if (participant == null || !participant.ParticipantShows.Any(ps => ps.ShowId == showId))
            {
                TempData["ErrorMessage"] = "You are not registered for this show.";
                return RedirectToAction("MyShows");
            }

            if (designerIds == null || !designerIds.Any())
            {
                TempData["ErrorMessage"] = "You must select at least one designer to vote for.";
                return RedirectToAction("Vote", new { showId });
            }

            foreach (var designerId in designerIds)
            {
                var existingVote = await _context.Votes
                    .FirstOrDefaultAsync(v => v.ParticipantId == participant.ParticipantId && v.DesignerId == designerId && v.ShowId == showId);

                if (existingVote == null)
                {
                    _context.Votes.Add(new Vote
                    {
                        ParticipantId = participant.ParticipantId,
                        DesignerId = designerId,
                        ShowId = showId,
                        VotedAt = DateTime.UtcNow
                    });
                }
            }

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Your vote has been submitted successfully!";
            return RedirectToAction("MyShows");
        }



        // ✅ ADMIN & PARTICIPANT: View Show Details
        [Authorize(Roles = "Admin,Participant")]
        public async Task<IActionResult> Details(int id)
        {
            var show = await _context.Shows
                .Include(s => s.DesignerShows)
                .ThenInclude(ds => ds.Designer)
                .Include(s => s.ParticipantShows)
                .ThenInclude(ps => ps.Participant)
                .FirstOrDefaultAsync(s => s.ShowId == id);

            if (show == null)
            {
                return NotFound("Show not found.");
            }

            // Check if the user is an Admin
            if (User.IsInRole("Admin"))
            {
                return View(show); // Admins can directly view the show details
            }

            // For Participants, ensure they are registered for this show
            var userEmail = User.Identity.Name;
            var participant = await _context.Participants
                .Include(p => p.ParticipantShows)
                .FirstOrDefaultAsync(p => p.Email == userEmail);

            if (participant == null || !participant.ParticipantShows.Any(ps => ps.ShowId == id))
            {
                TempData["ErrorMessage"] = "You are not registered for this show.";
                return RedirectToAction("MyShows");
            }

            return View(show); // Participants can view details only if registered
        }




    }
}
