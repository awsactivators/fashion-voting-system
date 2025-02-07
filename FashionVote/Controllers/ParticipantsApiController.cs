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

    public class ParticipantsApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ParticipantsApiController(ApplicationDbContext context)
        {
            _context = context;
        }



        /// <summary>
        /// Retrieves all participants with their registered shows.
        /// </summary>
        /// <returns>
        /// Returns a list of all participants along with their registered shows.
        /// </returns>
        /// <example>
        /// curl -X GET "http://localhost:5157/api/participants" \
            // -H "Content-Type: application/json"
        /// <output>{"participantId":1,"name":"Jane Doe","email":"janedoe@gmail.com","shows":[]}</output>
        /// </example>
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
        /// Retrieves details of a specific participant (Admin Only).
        /// </summary>
        /// <param name="id">The ID of the participant.</param>
        /// <returns>
        /// Returns JSON data of the participant or 404 if not found.
        /// </returns>
        /// <example>
        /// curl -X GET "http://localhost:5157/api/participants/1" \
            // -H "Content-Type: application/json"
        /// <output>{"participantId":1,"name":"Jane Doe","email":"janedoe@gmail.com","shows":[]}</output>
        /// </example>
        /// /// <example>
        /// curl -X GET "http://localhost:5157/api/participants/3" \
            // -H "Content-Type: application/json"
        /// <output>{"message":"Participant not found."}</output>
        /// </example>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetParticipantById(int id)
        {
            var participant = await _context.Participants
                .Include(p => p.ParticipantShows)
                .ThenInclude(ps => ps.Show)
                .FirstOrDefaultAsync(p => p.ParticipantId == id);

            if (participant == null)
                return NotFound(new { message = "Participant not found." });

            return Ok(new ParticipantDTO(participant));
        }



        /// <summary>
        /// Updates an existing participant's details.
        /// </summary>
        /// <param name="id">The ID of the participant to update.</param>
        /// <param name="participantDto">Updated participant details.</param>
        /// <returns>
        /// Returns a success message if the update is successful, or an error message if the participant is not found.
        /// </returns>
        /// <example>
        /// curl -X PUT "http://localhost:5157/api/participants/edit/1" \
            // -H "Content-Type: application/json" \
            // -d '{
            //       "participantId": 1,
            //       "name": "Jane Doe",
            //       "email": "janedoe@gmail.com",
            //       "selectedShowIds": [1, 3]
            //     }'
        /// <output>{"message":"Participant updated successfully!"}</output>
        /// </example>

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
        /// <param name="id">The ID of the participant to delete.</param>
        /// <returns>
        /// Returns a success message if the participant is deleted successfully, or an error message if the participant is not found.
        /// </returns>
        /// <example>
        /// curl -X DELETE "http://localhost:5157/api/participants/delete/1" \
            // -H "Content-Type: application/json"
        /// <output>{"message":"Participant deleted successfully."}</output>
        /// </example>
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
