using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FashionVote.Data;
using FashionVote.Models;
using System.Linq;
using System.Threading.Tasks;

namespace FashionVote.Controllers
{
    [Authorize]
    [Route("Shows")]
    public class ShowsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public ShowsController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [HttpGet("")]
        [AllowAnonymous]
        public async Task<IActionResult> Index()
        {
            var shows = await _context.Shows
                .AsNoTracking()
                .Where(s => s.EndTime > DateTime.UtcNow)
                .OrderBy(s => s.StartTime)
                .ToListAsync();

            return View(shows);
        }

        [HttpGet("admin")]
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

        [HttpGet("myshows")]
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

            return View(participant.ParticipantShows.Select(ps => ps.Show).ToList());
        }

        [HttpPost("register/{showId}")]
        [Authorize(Roles = "Participant")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(int showId)
        {
            var userEmail = User.Identity.Name;
            var participant = await _context.Participants
                .Include(p => p.ParticipantShows)
                .FirstOrDefaultAsync(p => p.Email == userEmail);

            if (participant == null)
            {
                TempData["ErrorMessage"] = "Only registered participants can register for shows.";
                return RedirectToAction(nameof(Index));
            }

            if (participant.ParticipantShows.Any(ps => ps.ShowId == showId))
            {
                TempData["ErrorMessage"] = "You have already registered for this show.";
                return RedirectToAction(nameof(Index));
            }

            var newShow = await _context.Shows.FindAsync(showId);
            if (newShow == null)
            {
                TempData["ErrorMessage"] = "The show does not exist.";
                return RedirectToAction(nameof(Index));
            }

            participant.ParticipantShows.Add(new ParticipantShow
            {
                ParticipantId = participant.ParticipantId,
                ShowId = showId
            });

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "You have successfully registered for the show!";
            return RedirectToAction(nameof(MyShows));
        }

        [HttpGet("create")]
        [Authorize(Roles = "Admin")]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost("create")]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Show show)
        {
            if (!ModelState.IsValid)
                return View(show);

            _context.Add(show);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Show added successfully!";
            return RedirectToAction(nameof(AdminIndex));
        }


        [HttpGet("edit/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id)
        {
            var show = await _context.Shows.FindAsync(id);
            if (show == null) return NotFound();
            return View(show);
        }

        [HttpPost("edit/{id}")]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [FromForm] Show show)
        {
            if (id != show.ShowId) return BadRequest();
            if (!ModelState.IsValid) return View(show);

            _context.Update(show);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Show updated successfully.";
            return RedirectToAction(nameof(AdminIndex));
        }


        /// <summary>
        /// Displays details of a specific show.
        /// </summary>
        [HttpGet("details/{id}")]
        [AllowAnonymous]
        public async Task<IActionResult> Details(int id)
        {
            var show = await _context.Shows
                .Include(s => s.DesignerShows)
                .ThenInclude(ds => ds.Designer)
                .FirstOrDefaultAsync(s => s.ShowId == id);

            if (show == null)
            {
                TempData["ErrorMessage"] = "Show not found.";
                return RedirectToAction(nameof(Index));
            }

            return View(show);
        }

        /// <summary>
        /// Unregisters a participant from a show before it starts.
        /// </summary>
        [HttpPost("unregister/{showId}")]
        [Authorize(Roles = "Participant")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Unregister(int showId)
        {
            var userEmail = User.Identity.Name;
            var participant = await _context.Participants
                .Include(p => p.ParticipantShows)
                .ThenInclude(ps => ps.Show)
                .FirstOrDefaultAsync(p => p.Email == userEmail);

            if (participant == null)
            {
                TempData["ErrorMessage"] = "Only registered participants can unregister.";
                return RedirectToAction(nameof(MyShows));
            }

            var show = participant.ParticipantShows.FirstOrDefault(ps => ps.ShowId == showId)?.Show;
            if (show == null)
            {
                TempData["ErrorMessage"] = "You are not registered for this show.";
                return RedirectToAction(nameof(MyShows));
            }

            if (show.StartTime <= DateTime.UtcNow)
            {
                TempData["ErrorMessage"] = "You cannot unregister from a show that has already started.";
                return RedirectToAction(nameof(MyShows));
            }

            participant.ParticipantShows.Remove(participant.ParticipantShows.First(ps => ps.ShowId == showId));
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "You have successfully unregistered from this show.";
            return RedirectToAction(nameof(MyShows));
        }

        /// <summary>
        /// Displays the delete confirmation page for a past show.
        /// </summary>
        [HttpGet("delete/participant/confirm/{showId}")]
        [Authorize(Roles = "Participant")]
        public async Task<IActionResult> DeleteConfirmParticipant(int showId)
        {
            var userEmail = User.Identity.Name;
            var participant = await _context.Participants
                .Include(p => p.ParticipantShows)
                .ThenInclude(ps => ps.Show)
                .FirstOrDefaultAsync(p => p.Email == userEmail);

            if (participant == null)
            {
                TempData["ErrorMessage"] = "You are not registered for any shows.";
                return RedirectToAction(nameof(MyShows));
            }

            var show = participant.ParticipantShows.FirstOrDefault(ps => ps.ShowId == showId)?.Show;
            if (show == null)
            {
                TempData["ErrorMessage"] = "Show not found.";
                return RedirectToAction(nameof(MyShows));
            }

            if (show.EndTime > DateTime.UtcNow)
            {
                TempData["ErrorMessage"] = "You can only delete past shows.";
                return RedirectToAction(nameof(MyShows));
            }

            return View(show);
        }

        /// <summary>
        /// Deletes a participant's past show.
        /// </summary>
        [HttpPost("participant/delete/confirmed/{showId}")]
        [Authorize(Roles = "Participant")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmedParticipant(int showId)
        {
            var userEmail = User.Identity.Name;
            var participant = await _context.Participants
                .Include(p => p.ParticipantShows)
                .ThenInclude(ps => ps.Show)
                .FirstOrDefaultAsync(p => p.Email == userEmail);

            if (participant == null)
            {
                TempData["ErrorMessage"] = "Only registered participants can delete past shows.";
                return RedirectToAction(nameof(MyShows));
            }

            var show = participant.ParticipantShows.FirstOrDefault(ps => ps.ShowId == showId)?.Show;
            if (show == null)
            {
                TempData["ErrorMessage"] = "You are not registered for this show.";
                return RedirectToAction(nameof(MyShows));
            }

            if (show.EndTime > DateTime.UtcNow)
            {
                TempData["ErrorMessage"] = "You can only delete past shows.";
                return RedirectToAction(nameof(MyShows));
            }

            participant.ParticipantShows.Remove(participant.ParticipantShows.First(ps => ps.ShowId == showId));
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Past show deleted successfully.";
            return RedirectToAction(nameof(MyShows));
        }

        [HttpGet("delete/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var show = await _context.Shows.FindAsync(id);
            return show == null ? NotFound() : View(show);
        }

        [HttpPost("delete/{id}")]
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
