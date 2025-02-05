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
    [Route("Designers")]
    public class DesignersController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DesignersController(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Retrieves a list of all designers with the shows they are participating in.
        /// </summary>
        /// <returns>Returns JSON (API) or the Index view.</returns>
        /// <example>GET /api/designers</example>
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var designers = await _context.Designers
                .Include(d => d.DesignerShows)
                .ThenInclude(ds => ds.Show)
                .ToListAsync();

            var designerDTOs = designers.Select(d => new DesignerDTO(d)).ToList();

            return Request.Headers["Accept"] == "application/json" ? Ok(designerDTOs) : View(designers);
        }

        /// <summary>
        /// Displays the form to create a new designer.
        /// </summary>
        /// <returns>Returns the Create view with only future shows.</returns>
        /// <example>GET /designers/create</example>
        [HttpGet("create")]
        public async Task<IActionResult> Create()
        {
            ViewBag.Shows = await _context.Shows
                .Where(s => s.EndTime > DateTime.UtcNow) // ✅ Exclude past shows
                .OrderBy(s => s.StartTime)
                .ToListAsync();

            return View();
        }


        /// <summary>
        /// Creates a new designer and assigns them to selected shows.
        /// Supports both JSON API and Razor view submissions.
        /// </summary>
        /// <param name="designerDto">DTO containing designer details and selected show IDs.</param>
        /// <returns>Returns JSON response for API or redirects to Index for Razor views.</returns>
        /// <example>POST /api/designers/create</example>
        [HttpPost("create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([FromForm] DesignerCreateDTO designerDto) // ✅ Change FromBody to FromForm
        {
            if (designerDto == null)
                return BadRequest(new { message = "Invalid data. Designer data is required." });

            if (!ModelState.IsValid)
            {
                ViewBag.Shows = await _context.Shows.ToListAsync();
                return Request.Headers["Accept"] == "application/json" ? BadRequest(ModelState) : View("Create");
            }

            try
            {
                var designer = new Designer
                {
                    Name = designerDto.Name,
                    Category = designerDto.Category
                };

                _context.Designers.Add(designer);
                await _context.SaveChangesAsync();

                if (designerDto.SelectedShowIds != null && designerDto.SelectedShowIds.Any())
                {
                    foreach (var showId in designerDto.SelectedShowIds)
                    {
                        _context.DesignerShows.Add(new DesignerShow
                        {
                            DesignerId = designer.DesignerId,
                            ShowId = showId
                        });
                    }
                    await _context.SaveChangesAsync();
                }

                if (Request.Headers["Accept"] == "application/json")
                {
                    return Ok(new { message = "Designer created successfully!", designerId = designer.DesignerId });
                }
                else
                {
                    TempData["SuccessMessage"] = "Designer added successfully!";
                    return RedirectToAction(nameof(Index));
                }
            }
            catch (Exception ex)
            {
                return Request.Headers["Accept"] == "application/json"
                    ? StatusCode(500, new { message = $"Internal Server Error: {ex.Message}" })
                    : View("Create");
            }
        }


        /// <summary>
        /// Displays the edit form for an existing designer.
        /// </summary>
        /// <param name="id">The ID of the designer to edit.</param>
        /// <returns>Returns the Edit view with prefilled data.</returns>
        /// <example>GET /designers/edit/5</example>
        [HttpGet("edit/{id}")]
        public async Task<IActionResult> Edit(int id)
        {
            var designer = await _context.Designers
                .Include(d => d.DesignerShows)
                .ThenInclude(ds => ds.Show)
                .FirstOrDefaultAsync(d => d.DesignerId == id);

            if (designer == null) return NotFound();

            ViewBag.ShowList = new MultiSelectList(_context.Shows, "ShowId", "ShowName",
                designer.DesignerShows.Select(ds => ds.ShowId));

            return View(designer);
        }

        /// <summary>
        /// Updates an existing designer's details and assigned shows.
        /// Supports both Razor View submissions and API JSON requests.
        /// </summary>
        /// <param name="id">The ID of the designer to update.</param>
        /// <param name="designerDto">Updated designer details.</param>
        /// <returns>Redirects to the Index view or returns JSON response.</returns>
        /// <example>POST /api/designers/edit/5 (Form Submission)</example>
        /// <example>PUT /api/designers/edit/5 (API JSON)</example>
        [HttpPost("edit/{id}")] // ✅ Change from HttpPut to HttpPost for form support
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [FromForm] DesignerUpdateDTO designerDto) // ✅ Use FromForm
        {
            if (id != designerDto.DesignerId) return BadRequest("ID mismatch.");

            var designer = await _context.Designers
                .Include(d => d.DesignerShows)
                .FirstOrDefaultAsync(d => d.DesignerId == id);

            if (designer == null) return NotFound("Designer not found.");

            if (!ModelState.IsValid)
            {
                ViewBag.ShowList = new MultiSelectList(_context.Shows, "ShowId", "ShowName", designerDto.SelectedShowIds);
                return View("Edit", designer); // ✅ Reload view with errors
            }

            try
            {
                // ✅ Update designer fields
                designer.Name = designerDto.Name;
                designer.Category = designerDto.Category;

                // ✅ Remove old show assignments
                _context.DesignerShows.RemoveRange(designer.DesignerShows);

                // ✅ Add new show assignments
                foreach (var showId in designerDto.SelectedShowIds ?? new int[0])
                {
                    _context.DesignerShows.Add(new DesignerShow
                    {
                        DesignerId = designer.DesignerId,
                        ShowId = showId
                    });
                }

                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Designers.Any(d => d.DesignerId == id)) return NotFound();
                throw;
            }

            TempData["SuccessMessage"] = "Designer updated successfully!";

            return Request.Headers["Accept"] == "application/json"
                ? Ok(new { message = "Designer updated successfully!" })
                : RedirectToAction(nameof(Index)); // ✅ Redirect after successful update
        }


        /// <summary>
        /// Displays the delete confirmation page for a designer.
        /// Supports both API (`GET /api/designers/delete/5`) and Razor View.
        /// </summary>
        /// <param name="id">The ID of the designer.</param>
        /// <returns>Returns the Delete view or JSON response.</returns>
        /// <example>GET /designers/delete/5</example>
        /// <example>GET /api/designers/delete/5</example>
        [HttpGet("delete/{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var designer = await _context.Designers
                .Include(d => d.DesignerShows)
                .ThenInclude(ds => ds.Show)
                .FirstOrDefaultAsync(d => d.DesignerId == id);

            if (designer == null) return NotFound();

            return Request.Headers["Accept"] == "application/json" 
                ? Ok(new DesignerDTO(designer)) 
                : View(designer);
        }

        /// <summary>
        /// Deletes a designer (Form Submission - Razor View).
        /// </summary>
        /// <param name="id">The ID of the designer.</param>
        /// <returns>Redirects to Index after deletion.</returns>
        /// <example>POST /designers/delete/5</example>
        [HttpPost("delete/{id}")]
        [ValidateAntiForgeryToken] // ✅ Form requests only
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var designer = await _context.Designers
                .Include(d => d.DesignerShows)
                .FirstOrDefaultAsync(d => d.DesignerId == id);

            if (designer == null) return NotFound();

            _context.DesignerShows.RemoveRange(designer.DesignerShows);
            _context.Designers.Remove(designer);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Designer deleted successfully!";
            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// Deletes a designer (API Request - JSON).
        /// </summary>
        /// <param name="id">The ID of the designer.</param>
        /// <returns>Returns JSON response.</returns>
        /// <example>DELETE /api/designers/5</example>
        [HttpDelete("delete/{id}")]
        public async Task<IActionResult> DeleteApi(int id)
        {
            var designer = await _context.Designers
                .Include(d => d.DesignerShows)
                .FirstOrDefaultAsync(d => d.DesignerId == id);

            if (designer == null) return NotFound(new { message = "Designer not found." });

            _context.DesignerShows.RemoveRange(designer.DesignerShows);
            _context.Designers.Remove(designer);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Designer deleted successfully." });
        }

        /// <summary>
        /// Retrieves details of a specific designer, including their assigned shows.
        /// </summary>
        /// <param name="id">The ID of the designer.</param>
        /// <returns>Returns JSON (API) or the Details view.</returns>
        /// <example>GET /api/designers/details/5</example>
        [HttpGet("details/{id}")]
        public async Task<IActionResult> Details(int id)
        {
            var designer = await _context.Designers
                .Include(d => d.DesignerShows)
                .ThenInclude(ds => ds.Show)
                .FirstOrDefaultAsync(d => d.DesignerId == id);

            if (designer == null)
            {
                return NotFound("Designer not found.");
            }

            var designerDTO = new DesignerDTO(designer);

            return Request.Headers["Accept"] == "application/json"
                ? Ok(designerDTO)
                : View(designer);
        }

    }
}
