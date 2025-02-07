using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FashionVote.Data;
using FashionVote.Models;
using FashionVote.DTOs;
using System.Linq;
using System.Threading.Tasks;
using FashionVote.Models.DTOs;


namespace FashionVote.Controllers
{
    [Authorize]
    [Route("Votes")]
    public class VotesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public VotesController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [HttpGet("")]
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

        [HttpGet("ShowVotes/{showId}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ShowVotes(int showId)
        {
            var show = await _context.Shows
                .Include(s => s.DesignerShows).ThenInclude(ds => ds.Designer)
                .Include(s => s.Votes)
                .FirstOrDefaultAsync(s => s.ShowId == showId);

            if (show == null)
            {
                TempData["ErrorMessage"] = "Show not found.";
                return RedirectToAction("Index", "Shows");
            }

            var designerVoteCounts = show.DesignerShows
                .Select(ds => new DesignerVoteDTO
                {
                    DesignerName = ds.Designer.Name,
                    Category = ds.Designer.Category,
                    VoteCount = show.Votes.Count(v => v.DesignerId == ds.Designer.DesignerId)
                })
                .OrderByDescending(dv => dv.VoteCount)
                .ToList();

            ViewBag.DesignerVotes = designerVoteCounts;
            return View(show);
        }

        [HttpGet("Vote/{showId}")]
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
                return RedirectToAction("MyShows", "Shows");
            }

            var show = await _context.Shows
                .Include(s => s.DesignerShows).ThenInclude(ds => ds.Designer)
                .Include(s => s.Votes)
                .FirstOrDefaultAsync(s => s.ShowId == showId);

            if (show == null)
            {
                TempData["ErrorMessage"] = "Show not found.";
                return RedirectToAction("MyShows", "Shows");
            }

            return View(show);
        }



         /// <summary>
        /// Submits votes for a show (For Razor Views).
        /// </summary>
        [HttpPost("SubmitVote")]
        [Authorize(Roles = "Participant")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitVote(int showId, List<int> DesignerIds)
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

            if (DesignerIds == null || !DesignerIds.Any())
            {
                TempData["ErrorMessage"] = "You must vote for at least one designer.";
                return RedirectToAction("Vote", new { showId });
            }

            foreach (var designerId in DesignerIds)
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
            
            // Redirect to the Vote page again to persist selections
            return RedirectToAction("Vote", new { showId });
        }



        /// <summary>
        /// Removes a vote for a designer in a show.
        /// </summary>
        [HttpPost("Unvote")]
        [Authorize(Roles = "Participant")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Unvote(int showId, int designerId)
        {
            var userEmail = User.Identity.Name;
            var participant = await _context.Participants
                .Include(p => p.Votes)
                .FirstOrDefaultAsync(p => p.Email == userEmail);

            if (participant == null)
            {
                TempData["ErrorMessage"] = "Only registered participants can unvote.";
                return RedirectToAction("Vote", new { showId });
            }

            var vote = await _context.Votes
                .FirstOrDefaultAsync(v => v.ParticipantId == participant.ParticipantId &&
                                        v.ShowId == showId &&
                                        v.DesignerId == designerId);

            if (vote == null)
            {
                TempData["ErrorMessage"] = "No vote found to remove.";
                return RedirectToAction("Vote", new { showId });
            }

            _context.Votes.Remove(vote);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Your vote has been removed successfully!";
            return RedirectToAction("Vote", new { showId });
        }

    }
}