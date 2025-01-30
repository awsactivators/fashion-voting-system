using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FashionVote.Data;
using FashionVote.Models;

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
        /// Submits a vote for a designer.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Vote(int participantId, int designerId, int showId)
        {
            var vote = new Vote
            {
                ParticipantId = participantId,
                DesignerId = designerId,
                ShowId = showId
            };

            _context.Votes.Add(vote);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Vote submitted successfully!";
            return RedirectToAction("Index", "Participants");
        }
    }
}
