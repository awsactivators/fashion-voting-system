using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FashionVote.Data;
using FashionVote.Models;
using FashionVote.DTOs;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.AspNetCore.Authorization;
using FashionVote.Models.DTOs;


namespace FashionVote.Controllers
{
    [ApiController]
    [Route("api/participants")]
    [Produces("application/json")]
    [Authorize(Roles = "Admin")] // Admin required for all API endpoints
    public class ParticipantsApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ParticipantsApiController(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Retrieves all participants with their registered shows (Admin Only).
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetParticipants()
        {
            var participants = await _context.Participants
                .Include(p => p.ParticipantShows)
                .ThenInclude(ps => ps.Show)
                .ToListAsync();

            var participantDTOs = participants.Select(p => new ParticipantDTO(p)).ToList();
            return Ok(participantDTOs);
        }

        /// <summary>
        /// Updates an existing participant's details (Admin Only).
        /// </summary>
        [HttpPut("edit/{id}")]
        public async Task<IActionResult> EditParticipant(int id, [FromBody] ParticipantUpdateDTO participantDto)
        {
            if (id != participantDto.ParticipantId) return BadRequest("ID mismatch.");

            var participant = await _context.Participants
                .Include(p => p.ParticipantShows)
                .FirstOrDefaultAsync(p => p.ParticipantId == id);

            if (participant == null) return NotFound("Participant not found.");

            participant.Name = participantDto.Name;
            participant.Email = participantDto.Email;

            _context.ParticipantShows.RemoveRange(participant.ParticipantShows);

            foreach (var showId in participantDto.SelectedShowIds ?? new int[0])
            {
                _context.ParticipantShows.Add(new ParticipantShow
                {
                    ParticipantId = participant.ParticipantId,
                    ShowId = showId
                });
            }

            await _context.SaveChangesAsync();
            return Ok(new { message = "Participant updated successfully!" });
        }

        /// <summary>
        /// Deletes a participant (Admin Only).
        /// </summary>
        [HttpDelete("delete/{id}")]
        public async Task<IActionResult> DeleteParticipant(int id)
        {
            var participant = await _context.Participants
                .Include(p => p.ParticipantShows)
                .FirstOrDefaultAsync(p => p.ParticipantId == id);

            if (participant == null) return NotFound(new { message = "Participant not found." });

            _context.ParticipantShows.RemoveRange(participant.ParticipantShows);
            _context.Participants.Remove(participant);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Participant deleted successfully." });
        }
    }
}
