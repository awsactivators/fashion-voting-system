using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FashionVote.Data;
using FashionVote.Models;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using FashionVote.Models.DTOs;
using FashionVote.DTOs;

namespace FashionVote.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
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

        /// <summary>
        /// Gets all votes for admin.
        /// </summary>
        /// <returns>Returns JSON (API) or Razor View.</returns>
        /// <example>GET /api/Votes</example>
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Index()
        {
            var votes = await _context.Votes
                .Include(v => v.Participant)
                .Include(v => v.Designer)
                .Include(v => v.Show)
                .ToListAsync();

            var voteDTOs = votes.Select(v => new VoteDTO(v)).ToList();

            return Request.Headers["Accept"] == "application/json" ? Ok(voteDTOs) : View(votes);
        }

        /// <summary>
        /// Displays votes for a specific show.
        /// </summary>
        /// <param name="showId">The ID of the show.</param>
        /// <returns>Returns a view of vote counts per designer.</returns>
        /// <example>GET /Votes/ShowVotes/5</example>
        [Authorize(Roles = "Admin")]
        [HttpGet("ShowVotes/{showId}")]
        public async Task<IActionResult> ShowVotes(int showId)
        {
            var show = await _context.Shows
                .Include(s => s.DesignerShows)
                    .ThenInclude(ds => ds.Designer)
                .Include(s => s.Votes)
                .FirstOrDefaultAsync(s => s.ShowId == showId);

            if (show == null)
            {
                return NotFound($"Show with ID {showId} not found.");
            }

            // ✅ Create a strongly-typed DTO instead of an anonymous type
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



        /// <summary>
        /// Loads the voting page.
        /// </summary>
        /// <param name="showId">Show ID.</param>
        /// <returns>Returns voting page.</returns>
        /// <example>GET /api/Votes/Vote/5</example>
        [HttpGet("Vote/{showId}")]
        [Authorize(Roles = "Participant")]
        public async Task<IActionResult> Vote(int showId)
        {
            var userEmail = User.Identity.Name;
            var participant = await _context.Participants
                .Include(p => p.ParticipantShows)
                .FirstOrDefaultAsync(p => p.Email == userEmail);

            if (participant == null || !participant.ParticipantShows.Any(ps => ps.ShowId == showId))
                return Unauthorized("You are not registered for this show.");

            var show = await _context.Shows
                .Include(s => s.DesignerShows).ThenInclude(ds => ds.Designer)
                .Include(s => s.Votes)
                .FirstOrDefaultAsync(s => s.ShowId == showId);

            return show == null ? NotFound("Show not found.") : View(show);
        }

        /// <summary>
        /// Submits votes for a show.
        /// </summary>
        /// <param name="voteDto">Vote submission DTO.</param>
        /// <returns>Returns success or failure response.</returns>
        /// <example>POST /api/Votes/SubmitVote</example>
        [HttpPost("SubmitVote")]
        [Authorize(Roles = "Participant")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitVote([FromForm] VoteSubmissionDTO voteDto)
        {
            var userEmail = User.Identity.Name;
            var participant = await _context.Participants
                .Include(p => p.ParticipantShows)
                .FirstOrDefaultAsync(p => p.Email == userEmail);

            if (participant == null || !participant.ParticipantShows.Any(ps => ps.ShowId == voteDto.ShowId))
                return Unauthorized("You are not registered for this show.");

            if (voteDto.DesignerIds == null || !voteDto.DesignerIds.Any())
                return BadRequest("You must select at least one designer to vote for.");

            foreach (var designerId in voteDto.DesignerIds)
            {
                var existingVote = await _context.Votes
                    .FirstOrDefaultAsync(v => v.ParticipantId == participant.ParticipantId && v.DesignerId == designerId && v.ShowId == voteDto.ShowId);

                if (existingVote == null)
                {
                    _context.Votes.Add(new Vote
                    {
                        ParticipantId = participant.ParticipantId,
                        DesignerId = designerId,
                        ShowId = voteDto.ShowId,
                        VotedAt = DateTime.UtcNow
                    });
                }
            }

            await _context.SaveChangesAsync();

            return Request.Headers["Accept"] == "application/json"
                ? Ok(new { message = "Vote submitted successfully!" })
                : RedirectToAction("Vote", new { showId = voteDto.ShowId });
        }

        /// <summary>
        /// Allows participants to remove their votes.
        /// </summary>
        /// <param name="voteDto">Vote removal DTO.</param>
        /// <returns>Returns success or failure response.</returns>
        /// <example>POST /api/Votes/Unvote</example>
        [HttpPost("Unvote")]
        [Authorize(Roles = "Participant")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Unvote([FromForm] VoteRemovalDTO voteDto)
        {
            var userEmail = User.Identity.Name;
            var participant = await _context.Participants
                .Include(p => p.Votes)
                .FirstOrDefaultAsync(p => p.Email == userEmail);

            if (participant == null) return Unauthorized("You are not registered for this show.");

            var vote = await _context.Votes
                .FirstOrDefaultAsync(v => v.ParticipantId == participant.ParticipantId &&
                                          v.ShowId == voteDto.ShowId &&
                                          v.DesignerId == voteDto.DesignerId);

            if (vote == null) return NotFound("No vote found to remove.");

            _context.Votes.Remove(vote);
            await _context.SaveChangesAsync();

            return Request.Headers["Accept"] == "application/json"
                ? Ok(new { message = "Vote removed successfully!" })
                : RedirectToAction("Vote", new { showId = voteDto.ShowId });
        }
    }
}
