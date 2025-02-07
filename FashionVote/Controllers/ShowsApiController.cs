using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FashionVote.Data;
using FashionVote.Models;
using FashionVote.Models.DTOs;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace FashionVote.Controllers.Api
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class ShowsApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public ShowsApiController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }



        /// <summary>
        /// Retrieves all upcoming shows.
        /// </summary>
        /// <returns>Returns a list of upcoming shows.</returns>
        /// <example>
        /// curl -X GET "http://localhost:5157/api/ShowsApi/list" -H "Accept: application/json"
        /// <output>{"showId":1,"showName":"Spring SS2","location":"Humber North","startTime":"2025-02-15T09:00:00","endTime":"2025-02-15T15:00:00","participantShows":[],"participants":[],"designerShows":[],"votes":[]}</output>
        /// </example>
        [HttpGet("list")]
        public async Task<IActionResult> GetShows()
        {
            var shows = await _context.Shows
                .AsNoTracking()
                .Where(s => s.EndTime > DateTime.UtcNow)
                .OrderBy(s => s.StartTime)
                .ToListAsync();

            if (!shows.Any()) return NotFound("No upcoming shows available.");

            return Ok(shows);
        }



        /// <summary>
        /// Retrieves all shows for admin management.
        /// </summary>
        /// <returns>Returns a list of all shows with participant and designer information.</returns>
        /// <example>
        /// curl -X GET "http://localhost:5157/api/ShowsApi/admin" -H "Accept: application/json"
        /// <output>{"showId":1,"showName":"Spring SS2","location":"Humber North","startTime":"2025-02-15T09:00:00","endTime":"2025-02-15T15:00:00"}</output>
        /// </example>
        [HttpGet("admin")]
        public async Task<IActionResult> AdminIndex()
        {
            var shows = await _context.Shows
                .Include(s => s.Participants)
                .Include(s => s.DesignerShows)
                .ThenInclude(ds => ds.Designer)
                .ToListAsync();

            return Ok(shows.Select(s => new ShowDto(s)));
        }


        /// <summary>
        /// Retrieves all shows that a participant has registered for.
        /// </summary>
        /// <returns>Returns a list of registered shows for the logged-in participant.</returns>
        /// <example>
        /// curl -X GET "http://localhost:5157/api/ShowsApi/myshows" -H "Accept: application/json"
        /// <output>{"showId":3,"showName":"Winter","location":"Humber Lakeshore","startTime":"2025-03-01T09:00:00","endTime":"2025-03-01T15:00:00"}</output>
        /// </example>
        [HttpGet("myshows")]
        public async Task<IActionResult> GetMyShows()
        {
            var userEmail = "luisdoe@gmail.com";
            var participant = await _context.Participants
                .Include(p => p.ParticipantShows)
                .ThenInclude(ps => ps.Show)
                .FirstOrDefaultAsync(p => p.Email == userEmail);

            if (participant == null || !participant.ParticipantShows.Any())
                return NotFound("You haven't registered for any shows yet.");

            return Ok(participant.ParticipantShows.Select(ps => new ShowDto(ps.Show)));
        }



        /// <summary>
        /// Retrieves details of a show by ID.
        /// </summary>
        /// <param name="id">The ID of the show.</param>
        /// <returns>Returns detailed information about the specified show.</returns>
        /// <example>
        /// curl -X GET "http://localhost:5157/api/ShowsApi/1" -H "Accept: application/json"
        /// <output>{"showId":1,"showName":"Spring SS2","location":"Humber North","startTime":"2025-02-15T09:00:00","endTime":"2025-02-15T15:00:00"}</output>
        /// </example>
        [HttpGet("{id}")]
        public async Task<IActionResult> Details(int id)
        {
            var show = await _context.Shows
                .Include(s => s.DesignerShows)
                .ThenInclude(ds => ds.Designer)
                .FirstOrDefaultAsync(s => s.ShowId == id);

            if (show == null) return NotFound("Show not found.");

            return Ok(new ShowDto(show));
        }



        /// <summary>
        /// Creates a new show.
        /// </summary>
        /// <param name="show">The show object containing name, location, and schedule.</param>
        /// <returns>Returns the created show details.</returns>
        /// <example>
        /// curl -X POST "http://localhost:5157/api/ShowsApi/create" \
            // -H "Content-Type: application/json" \
            // -H "Accept: application/json" \
            // -d '{
            //       "showName": "Toronto Fashion Show",
            //       "location": "Humber College",
            //       "startTime": "2025-02-07T19:15:00Z",
            //       "endTime": "2025-02-07T21:00:00Z"
            //     }'
        /// <output>{"Show created successfully."}</output>
        /// </example>
        [HttpPost("create")]
        public async Task<IActionResult> Create([FromBody] Show show)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            _context.Add(show);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(Details), new {message = "Show deleted successfully!", id = show.ShowId }, new ShowDto(show));
        }



        /// <summary>
        /// Updates an existing show.
        /// </summary>
        /// <param name="id">The ID of the show to update.</param>
        /// <param name="show">Updated show details.</param>
        /// <returns>Returns the updated show details.</returns>
        /// <example>
        /// curl -X PUT "http://localhost:5157/api/ShowsApi/edit/20" \
            //     -H "Content-Type: application/json" \
            //     -d '{
            //           "showId": 20,
            //           "showName": "Toronto Fashion Show",
            //           "location": "Eaton Center",
            //           "startTime": "2025-02-07T19:15:00Z",
            //           "endTime": "2025-02-07T21:00:00Z"
            //      }'
        /// <output>{"message":"Show updated successfully."}</output>
        /// </example>
        [HttpPut("edit/{id}")]
        public async Task<IActionResult> Edit(int id, [FromBody] Show show)
        {
            if (id != show.ShowId) return BadRequest("Mismatched Show ID.");
            if (!ModelState.IsValid) return BadRequest(ModelState);

            _context.Update(show);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Show updated successfully.", show });
        }



        /// <summary>
        /// Registers a participant for a show.
        /// </summary>
        /// <param name="showId">The ID of the show.</param>
        /// <returns>Returns success or error message.</returns>
        /// <example>
        /// curl -X POST "http://localhost:5157/api/ShowsApi/register/20" -H "Accept: application/json"
        /// <output>{"Successfully registered."}</output>
        /// </example>
        [HttpPost("register/{showId}")]
        public async Task<IActionResult> Register(int showId)
        {
            var userEmail = "luisdoe@gmail.com";
            var participant = await _context.Participants
                .Include(p => p.ParticipantShows)
                .FirstOrDefaultAsync(p => p.Email == userEmail);

            if (participant == null) return BadRequest("Only registered participants can register for shows.");
            if (participant.ParticipantShows.Any(ps => ps.ShowId == showId)) return BadRequest("Already registered.");

            var show = await _context.Shows.FindAsync(showId);
            if (show == null) return NotFound("Show does not exist.");

            _context.ParticipantShows.Add(new ParticipantShow { ParticipantId = participant.ParticipantId, ShowId = showId });
            await _context.SaveChangesAsync();

            return Ok("Successfully registered.");
        }



        /// <summary>
        /// Unregisters a participant from a show.
        /// </summary>
        /// <param name="showId">The ID of the show.</param>
        /// <returns>Returns success or error message.</returns>
        /// <example>
        /// curl -X POST "http://localhost:5157/api/ShowsApi/unregister/20" -H "Accept: application/json"
        /// <output>{"Successfully unregistered."}</output>
        /// </example>
        [HttpPost("unregister/{showId}")]
        public async Task<IActionResult> Unregister(int showId)
        {
            var userEmail = "luisdoe@gmail.com";
            var participant = await _context.Participants
                .Include(p => p.ParticipantShows)
                .FirstOrDefaultAsync(p => p.Email == userEmail);

            var participantShow = participant?.ParticipantShows.FirstOrDefault(ps => ps.ShowId == showId);
            if (participantShow == null) return NotFound("You are not registered for this show.");

            _context.ParticipantShows.Remove(participantShow);
            await _context.SaveChangesAsync();

            return Ok("Successfully unregistered.");
        }



        /// <summary>
        /// Deletes a participant's past registered show.
        /// </summary>
        /// <param name="showId">The ID of the show to be deleted.</param>
        /// <returns>Returns a success message if deleted.</returns>
        /// <example>
        /// curl -X DELETE "http://localhost:5157/api/ShowsApi/delete/20" -H "Accept: application/json"
        /// <output>{"Show deleted successfully."}</output>
        /// </example>
        [HttpDelete("deleteregistershow/{showId}")]
        public async Task<IActionResult> DeleteRegisterShow(int showId)
        {
            var userEmail = "luisdoe@gmail.com";
            var participant = await _context.Participants
                .Include(p => p.ParticipantShows)
                .ThenInclude(ps => ps.Show)
                .FirstOrDefaultAsync(p => p.Email == userEmail);

            if (participant == null)
            {
                return BadRequest("Only registered participants can delete past shows.");
            }

            Console.WriteLine($"Participant {participant.Email} is registered for {participant.ParticipantShows.Count} shows.");
            foreach (var ps in participant.ParticipantShows)
            {
                Console.WriteLine($"Show ID: {ps.ShowId}, Show Name: {ps.Show.ShowName}, End Time: {ps.Show.EndTime}");
            }

            var participantShow = participant.ParticipantShows.FirstOrDefault(ps => ps.ShowId == showId);

            if (participantShow == null)
            {
                return NotFound($"Show doesn't exist or You are not registered for this show (ID: {showId}).");
            }

            var show = participantShow.Show;

            if (show.EndTime > DateTime.UtcNow)
            {
                return BadRequest($"You can only delete past shows. {show.ShowName} ends at {show.EndTime}");
            }

            _context.ParticipantShows.Remove(participantShow);
            await _context.SaveChangesAsync();

            return Ok("Past show deleted successfully.");
        }




        /// <summary>
        /// Deletes a show permanently (Admin Only).
        /// </summary>
        /// <param name="id">The ID of the show.</param>
        /// <returns>Returns success or error message.</returns>
        /// <example>
        /// curl -X DELETE "http://localhost:5157/api/ShowsApi/delete/19" -H "Accept: application/json"
        /// <output>{"Show deleted successfully."}</output>
        /// </example>
        [HttpDelete("delete/{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var show = await _context.Shows.FindAsync(id);
            if (show == null) return NotFound("Show not found.");

            _context.Shows.Remove(show);
            await _context.SaveChangesAsync();

            return Ok("Show deleted successfully.");
        }
    }
}