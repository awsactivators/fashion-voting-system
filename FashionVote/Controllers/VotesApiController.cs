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


        /// <summary>
        /// Retrieves all votes cast by participants.
        /// </summary>
        /// <returns>
        /// A JSON response containing a list of votes.
        /// </returns>
        /// <example>
        /// curl -X GET "http://localhost:5157/api/VotesApi" -H "Accept: application/json"
        /// <output>{"voteId":1,"participantId":2,"participantName":"luisdoe","designerId":5,"designerName":"Belle Barbie","showId":7,"showName":"Young Famous","votedAt":"2025-02-03T00:59:40.712923"}</output>
        /// </example>
        [HttpGet]
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
        /// Retrieves voting details for a specific show, including vote counts for each designer.
        /// </summary>
        /// <param name="showId">The ID of the show to retrieve voting details for.</param>
        /// <returns>
        /// A JSON response containing the show details, including the number of votes received by each designer.
        /// </returns>
        /// <example>
        /// curl -X GET "http://localhost:5157/api/VotesApi/Vote/13" -H "Accept: application/json"
        /// <output>{"showId":13,"showName":"NFW","totalVotes":1,"designers":[{"designerId":4,"name":"Ella MIA","category":"Winter Jackets","voteCount":1},{"designerId":7,"name":"Obum Ife","category":"Bridal Wear","voteCount":0}]}</output>
        /// </example>
        [HttpGet("Vote/{showId}")]
        public async Task<IActionResult> GetVotePage(int showId)
        {
            var participant = await _context.Participants
                .Include(p => p.ParticipantShows)
                .FirstOrDefaultAsync(p => p.Email == "luisdoe@gmail.com");

            if (participant == null || !participant.ParticipantShows.Any(ps => ps.ShowId == showId))
            {
                return Unauthorized(new { message = "You are not registered for this show." });
            }

            var show = await _context.Shows
                .Include(s => s.DesignerShows).ThenInclude(ds => ds.Designer)
                .Include(s => s.Votes) 
                .FirstOrDefaultAsync(s => s.ShowId == showId);

            if (show == null)
            {
                return NotFound(new { message = "Show not found." });
            }

            var voteCounts = await _context.Votes
                .Where(v => v.ShowId == showId)
                .GroupBy(v => v.DesignerId)
                .Select(g => new
                {
                    DesignerId = g.Key,
                    VoteCount = g.Count()
                })
                .ToDictionaryAsync(v => v.DesignerId, v => v.VoteCount);

            var designers = show.DesignerShows.Select(ds => new
            {
                ds.Designer.DesignerId,
                ds.Designer.Name,
                ds.Designer.Category,
                VoteCount = voteCounts.ContainsKey(ds.Designer.DesignerId) ? voteCounts[ds.Designer.DesignerId] : 0 
            });

            return Ok(new
            {
                ShowId = show.ShowId,
                ShowName = show.ShowName,
                TotalVotes = show.Votes.Count, 
                Designers = designers
            });
        }




        /// <summary>
        /// Submits votes for a show.
        /// </summary>
        /// <param name="voteDto">An object containing the participant's vote data, including the show ID and selected designer IDs.</param>
        /// <returns>
        /// A JSON response indicating whether the vote was submitted successfully or if an error occurred.
        /// </returns>
        /// <example>
        /// POST /api/VotesApi/SubmitVote
        /// curl -X POST "http://localhost:5157/api/VotesApi/SubmitVote" \
            // -H "Content-Type: application/json" \
            // -H "Accept: application/json" \
            // -d '{
            //       "showId": 20,
            //       "designerIds": [2]
            //     }'
        /// <output>{"message":"Vote submitted successfully!"}</output>
        /// </example>
        [HttpPost("SubmitVote")]
          public async Task<IActionResult> SubmitVote([FromBody] VoteSubmissionDTO voteDto)
          {
              if (voteDto.ShowId == 0)
              {
                  return BadRequest(new { message = "Invalid show selection." });
              }

              if (voteDto.DesignerIds == null || !voteDto.DesignerIds.Any())
              {
                  return BadRequest(new { message = "You didn't vote for any designer." });
              }

              var participant = await _context.Participants
                  .Include(p => p.ParticipantShows)
                  .FirstOrDefaultAsync(p => p.Email == "luisdoe@gmail.com");

              if (participant == null || !participant.ParticipantShows.Any(ps => ps.ShowId == voteDto.ShowId))
              {
                  return Unauthorized(new { message = "You are not registered for this show." });
              }

              var show = await _context.Shows
                  .Include(s => s.DesignerShows)
                  .ThenInclude(ds => ds.Designer)
                  .FirstOrDefaultAsync(s => s.ShowId == voteDto.ShowId);

              if (show == null)
              {
                  return NotFound(new { message = "Show not found." });
              }

              if (!show.DesignerShows.Any())
              {
                  return BadRequest(new { message = "No designers are registered for this show. Voting is not possible." });
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



        /// <summary>
        /// Removes a participant's vote for a specific designer in a show.
        /// </summary>
        /// <param name="voteDto">An object containing the participant's vote data, including the show ID and designer ID.</param>
        /// <returns>
        /// A JSON response indicating whether the vote was removed successfully or if an error occurred.
        /// </returns>
        /// <example>
        /// curl -X POST "http://localhost:5157/api/VotesApi/Unvote" \
            // -H "Content-Type: application/json" \
            // -H "Accept: application/json" \
            // -d '{
            //       "showId": 20,
            //       "designerId": 2
            //     }'
        /// <output>{"message":"Vote removed successfully!"}</output>
        /// </example>
        [HttpPost("Unvote")]
        public async Task<IActionResult> Unvote([FromBody] VoteRemovalDTO voteDto)
        {
            var participant = await _context.Participants
                .Include(p => p.Votes)
                .FirstOrDefaultAsync(p => p.Email == "luisdoe@gmail.com");

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
