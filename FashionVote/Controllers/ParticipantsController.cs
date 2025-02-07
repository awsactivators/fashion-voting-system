using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using FashionVote.Data;
using FashionVote.Models;
using FashionVote.DTOs;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;

namespace FashionVote.Controllers
{
    [Authorize]
    [Route("participants")]
    public class ParticipantsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ParticipantsController(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Displays the list of participants (Accessible to all authenticated users).
        /// </summary>
        [HttpGet("")]
        public async Task<IActionResult> Index()
        {
            var participants = await _context.Participants
                .Include(p => p.ParticipantShows)
                .ThenInclude(ps => ps.Show)
                .ToListAsync();

            return View("Index", participants);
        }

        /// <summary>
        /// Displays the edit form for a participant (Admin Only).
        /// </summary>
        [HttpGet("edit/{id}")]
        [Authorize(Roles = "Admin")]
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
        /// Handles form submission for updating a participant (Admin Only).
        /// </summary>
        [HttpPost("edit/{id}")]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [FromForm] ParticipantUpdateDTO participantDto)
        {
            if (id != participantDto.ParticipantId) return BadRequest();

            var participant = await _context.Participants
                .Include(p => p.ParticipantShows)
                .FirstOrDefaultAsync(p => p.ParticipantId == id);

            if (participant == null) return NotFound();

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
            TempData["SuccessMessage"] = "Participant updated successfully!";
            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// Displays the delete confirmation page for a participant (Admin Only).
        /// </summary>
        [HttpGet("delete/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var participant = await _context.Participants
                .Include(p => p.ParticipantShows)
                .ThenInclude(ps => ps.Show)
                .FirstOrDefaultAsync(p => p.ParticipantId == id);

            if (participant == null) return NotFound();

            return View(participant);
        }

        /// <summary>
        /// Handles the form submission to delete a participant (Admin Only).
        /// </summary>
        [HttpPost("delete/{id}")]
        [Authorize(Roles = "Admin")]
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

        /// <summary>
        /// Displays the details of a participant (Accessible to all authenticated users).
        /// </summary>
        [HttpGet("details/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Details(int id)
        {
            var participant = await _context.Participants
                .Include(p => p.ParticipantShows)
                .ThenInclude(ps => ps.Show)
                .FirstOrDefaultAsync(p => p.ParticipantId == id);

            if (participant == null) return NotFound();

            return View(participant);
        }
    }
}
