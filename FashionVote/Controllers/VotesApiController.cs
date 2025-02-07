using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FashionVote.Data;
using FashionVote.Models;
using FashionVote.DTOs;
using System.Linq;
using System.Threading.Tasks;

namespace FashionVote.Controllers.Api
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class VotesApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public VotesApiController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetVotes()
        {
            var votes = await _context.Votes
                .Include(v => v.Participant)
                .Include(v => v.Designer)
                .Include(v => v.Show)
                .ToListAsync();

            var voteDTOs = votes.Select(v => new VoteDTO(v)).ToList();
            return Ok(voteDTOs);
        }


        /// <summary>
        /// Retrieves voting details for a show.
        /// </summary>
        [HttpGet("Vote/{showId}")]
        [Authorize(Roles = "Participant")]
        public async Task<IActionResult> GetVotePage(int showId)
        {
            var participant = await _context.Participants
                .Include(p => p.ParticipantShows)
                .FirstOrDefaultAsync(p => p.Email == User.Identity.Name);

            if (participant == null || !participant.ParticipantShows.Any(ps => ps.ShowId == showId))
            {
                return Unauthorized(new { message = "You are not registered for this show." });
            }

            var show = await _context.Shows
                .Include(s => s.DesignerShows).ThenInclude(ds => ds.Designer)
                .FirstOrDefaultAsync(s => s.ShowId == showId);

            if (show == null)
            {
                return NotFound(new { message = "Show not found." });
            }

            var designers = show.DesignerShows.Select(ds => new
            {
                ds.Designer.DesignerId,
                ds.Designer.Name,
                ds.Designer.Category
            });

            return Ok(new
            {
                ShowId = show.ShowId,
                ShowName = show.ShowName,
                Designers = designers
            });
        }


        /// <summary>
        /// Submits votes for a show.
        /// </summary>
        [HttpPost("SubmitVote")]
        [Authorize(Roles = "Participant")]
        public async Task<IActionResult> SubmitVote([FromBody] VoteSubmissionDTO voteDto)
        {
            if (voteDto.ShowId == 0 || voteDto.DesignerIds == null || !voteDto.DesignerIds.Any())
            {
                return BadRequest(new { message = "Invalid vote submission." });
            }

            var participant = await _context.Participants
                .Include(p => p.ParticipantShows)
                .FirstOrDefaultAsync(p => p.Email == User.Identity.Name);

            if (participant == null || !participant.ParticipantShows.Any(ps => ps.ShowId == voteDto.ShowId))
            {
                return Unauthorized(new { message = "You are not registered for this show." });
            }

            foreach (var designerId in voteDto.DesignerIds)
            {
                var existingVote = await _context.Votes
                    .FirstOrDefaultAsync(v => v.ParticipantId == participant.ParticipantId &&
                                              v.DesignerId == designerId &&
                                              v.ShowId == voteDto.ShowId);

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
            return Ok(new { message = "Vote submitted successfully!" });
        }


        [HttpPost("Unvote")]
        [Authorize(Roles = "Participant")]
        public async Task<IActionResult> Unvote([FromBody] VoteRemovalDTO voteDto)
        {
            var participant = await _context.Participants
                .Include(p => p.Votes)
                .FirstOrDefaultAsync(p => p.Email == User.Identity.Name);

            if (participant == null) return Unauthorized("You are not registered for this show.");

            var vote = await _context.Votes
                .FirstOrDefaultAsync(v => v.ParticipantId == participant.ParticipantId &&
                                          v.ShowId == voteDto.ShowId &&
                                          v.DesignerId == voteDto.DesignerId);

            if (vote == null) return NotFound("No vote found to remove.");

            _context.Votes.Remove(vote);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Vote removed successfully!" });
        }
    }
}
