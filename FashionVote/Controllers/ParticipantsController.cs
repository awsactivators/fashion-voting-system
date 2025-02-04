using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using FashionVote.Data;
using FashionVote.Models;
using Microsoft.AspNetCore.Authorization;
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
    [Route("Participants")]
    public class ParticipantsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ParticipantsController(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Retrieves a list of all participants and their registered shows.
        /// </summary>
        /// <returns>Returns JSON (API) or the Index view.</returns>
        /// <example>GET /api/participants</example>
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var participants = await _context.Participants
                .Include(p => p.ParticipantShows)
                .ThenInclude(ps => ps.Show)
                .ToListAsync();

            var participantDTOs = participants.Select(p => new ParticipantDTO(p)).ToList();

            return Request.Headers["Accept"] == "application/json" ? Ok(participantDTOs) : View(participants);
        }


        /// <summary>
        /// Displays the edit form for an existing participant.
        /// </summary>
        /// <param name="id">The ID of the participant to edit.</param>
        /// <returns>Returns the Edit view with prefilled data.</returns>
        /// <example>GET /participants/edit/5</example>
        [HttpGet("edit/{id}")]
        public async Task<IActionResult> Edit(int id)
        {
            var participant = await _context.Participants
                .Include(p => p.ParticipantShows)
                .ThenInclude(ps => ps.Show)
                .FirstOrDefaultAsync(p => p.ParticipantId == id);

            if (participant == null) return NotFound();

            ViewBag.ShowList = new MultiSelectList(_context.Shows, "ShowId", "ShowName",
                participant.ParticipantShows.Select(ps => ps.ShowId));

            return View(participant);
        }

        /// <summary>
        /// Updates an existing participant's details.
        /// </summary>
        /// <param name="id">The ID of the participant to update.</param>
        /// <param name="participantDto">Updated participant details.</param>
        /// <returns>Returns JSON response for API or redirects to Index for Razor views.</returns>
        /// <example>PUT /api/participants/edit/5</example>
        [HttpPut("edit/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [FromForm] ParticipantUpdateDTO participantDto)
        {
            if (id != participantDto.ParticipantId) return BadRequest("ID mismatch.");

            var participant = await _context.Participants
                .Include(p => p.ParticipantShows)
                .FirstOrDefaultAsync(p => p.ParticipantId == id);

            if (participant == null) return NotFound("Participant not found.");

            if (!ModelState.IsValid) return BadRequest(ModelState);

            try
            {
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
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Participants.Any(p => p.ParticipantId == id)) return NotFound();
                throw;
            }

            return Request.Headers["Accept"] == "application/json"
                ? Ok(new { message = "Participant updated successfully!" })
                : RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// Displays the delete confirmation page for a participant.
        /// </summary>
        /// <param name="id">The ID of the participant.</param>
        /// <returns>Returns the Delete view.</returns>
        /// <example>GET /participants/delete/5</example>
        [HttpGet("delete/{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var participant = await _context.Participants
                .Include(p => p.ParticipantShows)
                .ThenInclude(ps => ps.Show)
                .FirstOrDefaultAsync(p => p.ParticipantId == id);

            if (participant == null) return NotFound();

            return Request.Headers["Accept"] == "application/json"
                ? Ok(new ParticipantDTO(participant))
                : View(participant);
        }

        /// <summary>
        /// Deletes a participant.
        /// </summary>
        /// <param name="id">The ID of the participant.</param>
        /// <returns>Returns JSON response or redirects to Index.</returns>
        /// <example>DELETE /api/participants/5</example>
        [HttpDelete("delete/{id}")]
        public async Task<IActionResult> DeleteApi(int id)
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

        /// <summary>
        /// Confirms and deletes a participant (for Razor Views).
        /// </summary>
        /// <param name="id">The ID of the participant to delete.</param>
        /// <returns>Redirects to Index after deletion.</returns>
        /// <example>POST /participants/deleteconfirmed/5</example>
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var participant = await _context.Participants
                .Include(p => p.ParticipantShows)
                .FirstOrDefaultAsync(p => p.ParticipantId == id);

            if (participant == null) return NotFound();

            _context.ParticipantShows.RemoveRange(participant.ParticipantShows);
            _context.Participants.Remove(participant);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Participant deleted successfully!";
            return RedirectToAction(nameof(Index));
        }

    }
}
