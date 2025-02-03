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

        // ✅ ADMIN: View Votes for a Specific Show
        [Authorize(Roles = "Admin")]
        [Route("Votes/ShowVotes/{showId}")]
        public async Task<IActionResult> ShowVotes(int showId)
        {
            Console.WriteLine($"🔍 Debug: Searching for Show with ID {showId}");

            var show = await _context.Shows
                .Include(s => s.DesignerShows)
                    .ThenInclude(ds => ds.Designer)
                .Include(s => s.Votes)
                    .ThenInclude(v => v.Designer)
                .FirstOrDefaultAsync(s => s.ShowId == showId);

            if (show == null)
            {
                Console.WriteLine("❌ Debug: Show Not Found!");
                return NotFound($"Show with ID {showId} not found.");
            }

            // Get designers and vote counts
            var designerVoteCounts = show.DesignerShows
                .Select(ds => new
                {
                    Designer = ds.Designer,
                    VoteCount = show.Votes.Count(v => v.DesignerId == ds.Designer.DesignerId)
                })
                .OrderByDescending(dv => dv.VoteCount)
                .ToList();

            ViewBag.DesignerVotes = designerVoteCounts;
            return View(show);
        }



        // ✅ PARTICIPANTS: View Vote Page
        [Authorize(Roles = "Participant")]
        public async Task<IActionResult> Vote(int showId)
        {
            var userEmail = User.Identity.Name;
            var participant = await _context.Participants
                .Include(p => p.ParticipantShows)
                .FirstOrDefaultAsync(p => p.Email == userEmail);

            if (participant == null || !participant.ParticipantShows.Any(ps => ps.ShowId == showId))
            {
                TempData["ErrorMessage"] = "You are not registered for this show.";
                return RedirectToAction("MyShows", "Shows");
            }

            var show = await _context.Shows
                .Include(s => s.DesignerShows)
                .ThenInclude(ds => ds.Designer)
                .Include(s => s.Votes)
                .FirstOrDefaultAsync(s => s.ShowId == showId);

            if (show == null)
            {
                return NotFound("Show not found.");
            }

            return View(show);
        }

        // ✅ PARTICIPANTS: Submit Votes
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
                ViewData["ErrorMessage"] = "You are not registered for this show.";
                return RedirectToAction("Vote", new { showId });
            }

            if (designerIds == null || !designerIds.Any())
            {
                ViewData["InfoMessage"] = "You have chosen not to vote for any designer.";
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
            ViewData["SuccessMessage"] = "Your vote has been submitted successfully!";
            return RedirectToAction("Vote", new { showId });
        }


        // ✅ PARTICIPANTS: Unvote
        [HttpPost]
        [Authorize(Roles = "Participant")]
        public async Task<IActionResult> Unvote(int showId, int designerId)
        {
            var userEmail = User.Identity.Name;
            var participant = await _context.Participants
                .Include(p => p.Votes)
                .FirstOrDefaultAsync(p => p.Email == userEmail);

            if (participant == null)
            {
                ViewData["ErrorMessage"] = "You are not registered for this show.";
                return RedirectToAction("Vote", new { showId });
            }

            // ✅ Fetch the correct vote entry
            var vote = await _context.Votes
                .Where(v => v.Participant.Email == userEmail && v.ShowId == showId && v.DesignerId == designerId)
                .FirstOrDefaultAsync();

            if (vote == null)
            {
                ViewData["ErrorMessage"] = "No vote found to remove.";
                return RedirectToAction("Vote", new { showId });
            }

            // ✅ Remove the vote
            _context.Votes.Remove(vote);
            await _context.SaveChangesAsync();

            ViewData["SuccessMessage"] = "Your vote has been removed successfully!";
            return RedirectToAction("Vote", new { showId });
        }

    }
}
