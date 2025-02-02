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
    public class VotesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public VotesController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // ✅ ADMIN: View All Votes
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Index()
        {
            var votes = await _context.Votes
                .Include(v => v.Participant)
                .Include(v => v.Designer)
                .Include(v => v.Show)
                .ToListAsync();

            return View(votes);
        }


        // ✅ PARTICIPANTS: Vote for Designers in Registered Show
        [Authorize(Roles = "Participant")]
        [HttpPost]
        public async Task<IActionResult> Vote(int showId, int[] designerIds)
        {
            var userEmail = User.Identity.Name;
            var participant = await _context.Participants.FirstOrDefaultAsync(p => p.Email == userEmail);

            if (participant == null)
            {
                TempData["ErrorMessage"] = "Only registered participants can vote.";
                return RedirectToAction("MyShows", "Shows");
            }

            if (participant.ShowId != showId)
            {
                TempData["ErrorMessage"] = "You can only vote in the show you registered for.";
                return RedirectToAction("MyShows", "Shows");
            }

            var show = await _context.Shows.FindAsync(showId);
            if (show == null) return NotFound("Show not found.");

            // ✅ Prevent Voting Outside of Show Time
            if (DateTime.UtcNow < show.StartTime)
            {
                TempData["ErrorMessage"] = "Voting has not started yet!";
                return RedirectToAction("MyShows", "Shows");
            }
            if (DateTime.UtcNow > show.EndTime)
            {
                TempData["ErrorMessage"] = "Voting is closed!";
                return RedirectToAction("MyShows", "Shows");
            }

            // ✅ Prevent Duplicate Voting
            foreach (var designerId in designerIds)
            {
                var existingVote = await _context.Votes
                    .FirstOrDefaultAsync(v => v.ParticipantId == participant.ParticipantId && v.DesignerId == designerId && v.ShowId == showId);

                if (existingVote != null)
                {
                    TempData["ErrorMessage"] = "You have already voted for some designers.";
                    return RedirectToAction("MyShows", "Shows");
                }

                _context.Votes.Add(new Vote
                {
                    ParticipantId = participant.ParticipantId,
                    DesignerId = designerId,
                    ShowId = showId
                });
            }

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Your vote has been submitted successfully!";
            return RedirectToAction("MyShows", "Shows");
        }
    }
}
