using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FashionVote.Data;
using FashionVote.Models;
using FashionVote.DTOs;
using FashionVote.Models.DTOs;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace FashionVote.Controllers
{
    [ApiController]
    [Route("Shows")]
    [Route("api/[controller]")]
    public class ShowsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public ShowsController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        /// <summary>
        /// Returns a list of upcoming shows (Razor View).
        /// </summary>
        /// <returns>View with upcoming shows.</returns>
        [HttpGet("")]
        [AllowAnonymous]
        public async Task<IActionResult> Index()
        {
            Console.WriteLine("🔍 Fetching Upcoming Shows...");

            var shows = await _context.Shows
                .AsNoTracking() // Prevents cached data
                .Where(s => s.StartTime > DateTime.UtcNow) 
                .OrderBy(s => s.StartTime)
                .ToListAsync();

            if (!shows.Any())
            {
                Console.WriteLine("⚠️ No upcoming shows found!");
            }
            else
            {
                Console.WriteLine($"✅ {shows.Count} upcoming shows retrieved.");
            }

            return View(shows);
        }

        /// <summary>
        /// Returns a list of upcoming shows (API JSON).
        /// </summary>
        /// <returns>JSON List of upcoming shows.</returns>
        [HttpGet("api/list")]
        [AllowAnonymous]
        public async Task<IActionResult> GetShows()
        {
            Console.WriteLine("🔍 Fetching Upcoming Shows (API)...");

            var shows = await _context.Shows
                .AsNoTracking() // Prevents cached data
                .Where(s => s.StartTime > DateTime.UtcNow)
                .OrderBy(s => s.StartTime)
                .ToListAsync();

            if (!shows.Any())
            {
                Console.WriteLine("⚠️ No upcoming shows found in API response!");
            }
            else
            {
                Console.WriteLine($"✅ {shows.Count} upcoming shows retrieved for API.");
            }

            return Ok(shows);
        }




        /// <summary>
        /// Retrieves all shows for admin management.
        /// </summary>
        /// <returns>Returns a list of all shows.</returns>
        /// <example>GET /api/shows/admin</example>
        [HttpGet("admin")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AdminIndex()
        {
            var shows = await _context.Shows
                .Include(s => s.Participants)
                .Include(s => s.DesignerShows)
                .ThenInclude(ds => ds.Designer)
                .ToListAsync();

            if (Request.Headers["Accept"] == "application/json")
            {
                return Ok(shows.Select(s => new ShowDto(s)));
            }

            return View(shows);
        }

        /// <summary>
        /// Gets the shows registered by a participant.
        /// </summary>
        /// <returns>Returns the participant's registered shows.</returns>
        /// <example>GET /api/shows/myshows</example>
        [HttpGet("myshows")]
        [Authorize(Roles = "Participant")]
        public async Task<IActionResult> MyShows()
        {
            var userEmail = User.Identity.Name;

            var participant = await _context.Participants
                .Include(p => p.ParticipantShows)
                .ThenInclude(ps => ps.Show)
                .FirstOrDefaultAsync(p => p.Email == userEmail);

            if (participant == null || !participant.ParticipantShows.Any())
            {
                if (Request.Headers["Accept"] == "application/json")
                {
                    return NotFound("You haven't registered for any shows yet.");
                }

                TempData["ErrorMessage"] = "You haven't registered for any shows yet.";
                return RedirectToAction("Index");
            }

            var registeredShows = participant.ParticipantShows.Select(ps => ps.Show).ToList();

            if (Request.Headers["Accept"] == "application/json")
            {
                return Ok(registeredShows.Select(s => new ShowDto(s)));
            }

            return View(registeredShows);
        }

        /// <summary>
        /// Registers a participant for a show (Supports Razor View & API JSON).
        /// </summary>
        /// <param name="showId">ID of the show to register.</param>
        /// <returns>Redirects to MyShows or returns JSON response.</returns>
        /// <example>POST /api/shows/register/5</example>
        [HttpPost("register/{showId}")]
        [Authorize(Roles = "Participant")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(int showId)
        {
            var userEmail = User.Identity.Name;

            Console.WriteLine($"🔹 Registration attempt by {userEmail} for Show ID: {showId}");

            var participant = await _context.Participants
                .Include(p => p.ParticipantShows)
                .ThenInclude(ps => ps.Show)
                .FirstOrDefaultAsync(p => p.Email == userEmail);

            if (participant == null)
            {
                Console.WriteLine("❌ Participant not found.");
                return Request.Headers["Accept"] == "application/json"
                    ? BadRequest(new { message = "Only registered participants can register for shows." })
                    : RedirectToAction(nameof(Index), "Shows");
            }

            if (participant.ParticipantShows.Any(ps => ps.ShowId == showId))
            {
                Console.WriteLine("⚠️ Already registered for this show.");
                return Request.Headers["Accept"] == "application/json"
                    ? Conflict(new { message = "You are already registered for this show." })
                    : RedirectToAction(nameof(MyShows));
            }

            var newShow = await _context.Shows.FindAsync(showId);
            if (newShow == null)
            {
                Console.WriteLine("❌ Show not found.");
                return Request.Headers["Accept"] == "application/json"
                    ? NotFound(new { message = "The show does not exist." })
                    : RedirectToAction(nameof(Index), "Shows");
            }

            if (participant.ParticipantShows.Any(ps => 
                ps.Show.StartTime < newShow.EndTime && ps.Show.EndTime > newShow.StartTime))
            {
                Console.WriteLine("⚠️ Scheduling conflict detected.");
                return Request.Headers["Accept"] == "application/json"
                    ? Conflict(new { message = "You cannot register due to a scheduling conflict." })
                    : RedirectToAction(nameof(MyShows));
            }

            // ✅ Register the participant
            participant.ParticipantShows.Add(new ParticipantShow
            {
                ParticipantId = participant.ParticipantId,
                ShowId = showId
            });

            await _context.SaveChangesAsync();

            Console.WriteLine("✅ Registration successful!");

            return Request.Headers["Accept"] == "application/json"
                ? Ok(new { message = "You have successfully registered for this show." })
                : RedirectToAction(nameof(MyShows));
        }


        /// <summary>
        /// Loads the Create Show Form (Razor View).
        /// </summary>
        /// <returns>Returns the Create view.</returns>
        /// <example>GET /Shows/Create</example>
        [HttpGet("Create")]
        [Authorize(Roles = "Admin")]
        public IActionResult Create()
        {
            return View();
        }

        /// <summary>
        /// Creates a new show (Supports Razor View & API JSON).
        /// </summary>
        /// <param name="show">Show details.</param>
        /// <returns>Redirects to AdminIndex or returns JSON response.</returns>
        /// <example>POST /api/Shows/Create (JSON)</example>
        /// <example>POST /Shows/Create (Form)</example>
        [HttpPost("Create")]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([FromForm] Show show) // ✅ Handle HTML Form
        {
            Console.WriteLine($"🟡 Before Conversion: StartTime = {show.StartTime}, EndTime = {show.EndTime}");

            // ✅ Ensure Proper UTC Conversion
            show.StartTime = show.StartTime.Kind == DateTimeKind.Utc ? show.StartTime : show.StartTime.ToUniversalTime();
            show.EndTime = show.EndTime.Kind == DateTimeKind.Utc ? show.EndTime : show.EndTime.ToUniversalTime();

            Console.WriteLine($"🟢 After Conversion (UTC): StartTime = {show.StartTime}, EndTime = {show.EndTime}");
            Console.WriteLine($"⏳ Current UTC Time: {DateTime.UtcNow}");

            if (show.StartTime <= DateTime.UtcNow)
            {
                ModelState.AddModelError("StartTime", $"Start Time must be in the future! Current UTC Time: {DateTime.UtcNow}");
            }

            if (show.StartTime >= show.EndTime)
            {
                ModelState.AddModelError("EndTime", "End Time must be after Start Time!");
            }

            if (!ModelState.IsValid)
            {
                if (Request.Headers["Accept"] == "application/json")
                {
                    return BadRequest(ModelState);
                }
                return View(show);
            }

            _context.Add(show);
            await _context.SaveChangesAsync();

            Console.WriteLine("✅ Show successfully added!");

            if (Request.Headers["Accept"] == "application/json")
            {
                return CreatedAtAction(nameof(Details), new { id = show.ShowId }, new ShowDto(show));
            }

            TempData["SuccessMessage"] = "Show added successfully!";
            return RedirectToAction(nameof(AdminIndex));
        }



        /// <summary>
        /// Returns details of a show.
        /// </summary>
        /// <param name="id">Show ID.</param>
        /// <returns>Details view or JSON response.</returns>
        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<IActionResult> Details(int id)
        {
            var show = await _context.Shows
                .Include(s => s.DesignerShows)
                .ThenInclude(ds => ds.Designer)
                .FirstOrDefaultAsync(s => s.ShowId == id);

            if (show == null)
            {
                return NotFound("Show not found.");
            }

            if (Request.Headers["Accept"] == "application/json")
            {
                return Ok(new ShowDto(show));
            }

            return View(show);
        }

        
        /// <summary>
        /// Displays the edit form for a specific show.
        /// </summary>
        /// <param name="id">Show ID</param>
        /// <returns>Edit view or NotFound if show doesn't exist.</returns>
        /// <example>GET /Shows/Edit/8</example>
        [HttpGet("Edit/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id)
        {
            var show = await _context.Shows.FindAsync(id);
            if (show == null)
            {
                return NotFound();
            }

            return View(show); // ✅ Supports Razor Views
        }

        /// <summary>
        /// Updates an existing show (Supports both API & Razor Views).
        /// </summary>
        /// <param name="id">Show ID</param>
        /// <param name="show">Updated Show object</param>
        /// <returns>Redirects to AdminIndex (Razor) or Returns JSON (API)</returns>
        /// <example>PUT /api/Shows/Edit/8</example>
        [HttpPost("Edit/{id}")]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken] // ✅ Required for Razor Form
        public async Task<IActionResult> Edit(int id, [FromForm] Show show) // ✅ [FromForm] for Razor View Support
        {
            if (id != show.ShowId)
            {
                return BadRequest(new { message = "Mismatched Show ID." });
            }

            if (!ModelState.IsValid)
            {
                return View(show); // ✅ For Razor Views
            }

            try
            {
                _context.Update(show);
                await _context.SaveChangesAsync();

                if (Request.Headers["Accept"] == "application/json")
                {
                    return Ok(new { message = "Show updated successfully!", show });
                }

                TempData["SuccessMessage"] = "Show updated successfully!";
                return RedirectToAction(nameof(AdminIndex)); // ✅ Redirects Razor View
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Shows.Any(e => e.ShowId == id))
                {
                    return NotFound();
                }
                throw;
            }
        }


        /// <summary>
        /// Displays the delete confirmation page for a show.
        /// </summary>
        /// <param name="id">The ID of the show to delete.</param>
        /// <returns>Returns the Delete view with show details.</returns>
        /// <example>GET /api/shows/delete/5</example>
        [HttpGet("delete/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var show = await _context.Shows.FindAsync(id);
            if (show == null)
            {
                return NotFound("Show not found.");
            }

            return Request.Headers["Accept"] == "application/json" ? Ok(new ShowDto(show)) : View(show);
        }

        /// <summary>
        /// Deletes a show permanently.
        /// Supports both API calls and Razor form submissions.
        /// </summary>
        /// <param name="id">The ID of the show to delete.</param>
        /// <returns>Redirects to AdminIndex if successful.</returns>
        /// <example>DELETE /api/shows/delete/5</example>
        [HttpPost("delete/{id}")] // ✅ Changed to POST for Razor Forms
        [HttpDelete("delete/{id}")] // ✅ Still supports API DELETE requests
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken] // ✅ Works only for POST (Razor View support)
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var show = await _context.Shows.FindAsync(id);
            if (show == null)
            {
                return NotFound(new { message = "Show not found." });
            }

            _context.Shows.Remove(show);
            await _context.SaveChangesAsync();

            if (Request.Headers["Accept"] == "application/json")
            {
                return Ok(new { message = "Show deleted successfully." });
            }

            TempData["SuccessMessage"] = "Show deleted successfully!";
            return RedirectToAction(nameof(AdminIndex)); // ✅ Redirects for Razor View
        }



    }
}