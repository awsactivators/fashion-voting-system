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
        [HttpGet("list")]
        [AllowAnonymous]
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
        [HttpGet("admin")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AdminIndex()
        {
            var shows = await _context.Shows
                .Include(s => s.Participants)
                .Include(s => s.DesignerShows)
                .ThenInclude(ds => ds.Designer)
                .ToListAsync();

            return Ok(shows.Select(s => new ShowDto(s)));
        }


        [HttpGet("myshows")]
        [Authorize(Roles = "Participant")]
        public async Task<IActionResult> GetMyShows()
        {
            var userEmail = User.Identity.Name;
            var participant = await _context.Participants
                .Include(p => p.ParticipantShows)
                .ThenInclude(ps => ps.Show)
                .FirstOrDefaultAsync(p => p.Email == userEmail);

            if (participant == null || !participant.ParticipantShows.Any())
                return NotFound("You haven't registered for any shows yet.");

            return Ok(participant.ParticipantShows.Select(ps => new ShowDto(ps.Show)));
        }

        /// <summary>
        /// Retrieves details of a show.
        /// </summary>
        [HttpGet("{id}")]
        [AllowAnonymous]
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
        [HttpPost("create")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create([FromBody] Show show)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            _context.Add(show);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(Details), new { id = show.ShowId }, new ShowDto(show));
        }

        /// <summary>
        /// Updates an existing show.
        /// </summary>
        [HttpPut("edit/{id}")]
        [Authorize(Roles = "Admin")]
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
        [HttpPost("register/{showId}")]
        [Authorize(Roles = "Participant")]
        public async Task<IActionResult> Register(int showId)
        {
            var userEmail = User.Identity.Name;
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
        [HttpPost("unregister/{showId}")]
        [Authorize(Roles = "Participant")]
        public async Task<IActionResult> Unregister(int showId)
        {
            var userEmail = User.Identity.Name;
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
        /// Deletes a participant's past show.
        /// </summary>
        [HttpDelete("deleteregistershow/{showId}")]
        [Authorize(Roles = "Participant")]
        public async Task<IActionResult> DeleteRegisterShow(int showId)
        {
            var userEmail = User.Identity.Name;
            var participant = await _context.Participants
                .Include(p => p.ParticipantShows)
                .ThenInclude(ps => ps.Show)
                .FirstOrDefaultAsync(p => p.Email == userEmail);

            if (participant == null) return BadRequest("Only registered participants can delete past shows.");
            
            var show = participant.ParticipantShows.FirstOrDefault(ps => ps.ShowId == showId)?.Show;
            if (show == null) return NotFound("You are not registered for this show.");
            if (show.EndTime > DateTime.UtcNow) return BadRequest("You can only delete past shows.");

            participant.ParticipantShows.Remove(participant.ParticipantShows.First(ps => ps.ShowId == showId));
            await _context.SaveChangesAsync();

            return Ok("Past show deleted successfully.");
        }

        /// <summary>
        /// Deletes a show permanently.
        /// </summary>
        [HttpDelete("delete/{id}")]
        [Authorize(Roles = "Admin")]
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