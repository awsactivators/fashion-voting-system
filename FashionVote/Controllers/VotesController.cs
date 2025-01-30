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

        public VotesController(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Handles voting for a designer in a show by a participant.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Vote(int participantId, int showId, List<int> designerIds)
        {
            var participant = await _context.Participants.FindAsync(participantId);
            if (participant == null)
            {
                TempData["ErrorMessage"] = "Participant not found.";
                return RedirectToAction("Details", "Shows", new { id = showId });
            }

            if (designerIds == null || !designerIds.Any())
            {
                TempData["ErrorMessage"] = "You must select at least one designer to vote.";
                return RedirectToAction("Details", "Shows", new { id = showId });
            }

            var existingVotes = await _context.Votes
                .Where(v => v.ParticipantId == participantId && v.ShowId == showId)
                .Select(v => v.DesignerId)
                .ToListAsync();

            foreach (var designerId in designerIds)
            {
                if (!existingVotes.Contains(designerId)) // ✅ Skip if already voted
                {
                    var vote = new Vote
                    {
                        ParticipantId = participantId,
                        DesignerId = designerId,
                        ShowId = showId,
                        VotedAt = DateTime.UtcNow
                    };

                    _context.Votes.Add(vote);
                }
            }

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Your vote(s) have been submitted successfully!";

            return RedirectToAction("Details", "Shows", new { id = showId });
        }


    }
}
