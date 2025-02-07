using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using FashionVote.Data;
using FashionVote.Models;
using FashionVote.Models.DTOs;
using System.Linq;
using System.Threading.Tasks;

namespace FashionVote.Controllers
{
    [Authorize] // Requires authentication for all methods in this controller
    [Route("designers")]
    public class DesignersController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DesignersController(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Displays the list of designers (Accessible to all authenticated users).
        /// </summary>
        [HttpGet("")]
        public async Task<IActionResult> Index()
        {
            var designers = await _context.Designers
                .Include(d => d.DesignerShows)
                .ThenInclude(ds => ds.Show)
                .ToListAsync();

            return View("Index", designers);
        }

        /// <summary>
        /// Displays the form to create a new designer (Admin Only).
        /// </summary>
        [HttpGet("create")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create()
        {
            ViewBag.Shows = await _context.Shows
                .Where(s => s.EndTime > DateTime.UtcNow)
                .OrderBy(s => s.StartTime)
                .ToListAsync();

            return View();
        }

        /// <summary>
        /// Handles form submission for creating a new designer (Admin Only).
        /// </summary>
        [HttpPost("create")]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([FromForm] DesignerCreateDTO designerDto)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Shows = await _context.Shows.ToListAsync();
                return View("Create");
            }

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

            TempData["SuccessMessage"] = "Designer added successfully!";
            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// Displays the edit form for a designer (Admin Only).
        /// </summary>
        [HttpGet("edit/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id)
        {
            var designer = await _context.Designers
                .Include(d => d.DesignerShows)
                .ThenInclude(ds => ds.Show)
                .FirstOrDefaultAsync(d => d.DesignerId == id);

            if (designer == null) return NotFound();

            ViewBag.ShowList = new MultiSelectList(_context.Shows, "ShowId", "ShowName", designer.DesignerShows.Select(ds => ds.ShowId));
            return View(designer);
        }

        /// <summary>
        /// Updates a designer's details (Admin Only).
        /// </summary>
        [HttpPost("edit/{id}")]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [FromForm] DesignerUpdateDTO designerDto)
        {
            if (id != designerDto.DesignerId) return BadRequest();

            var designer = await _context.Designers
                .Include(d => d.DesignerShows)
                .FirstOrDefaultAsync(d => d.DesignerId == id);

            if (designer == null) return NotFound();

            designer.Name = designerDto.Name;
            designer.Category = designerDto.Category;
            _context.DesignerShows.RemoveRange(designer.DesignerShows);

            foreach (var showId in (designerDto.SelectedShowIds ?? new List<int>()))
            {
                _context.DesignerShows.Add(new DesignerShow
                {
                    DesignerId = designer.DesignerId,
                    ShowId = showId
                });
            }

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Designer updated successfully!";
            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// Displays the delete confirmation page for a designer (Admin Only).
        /// </summary>
        [HttpGet("delete/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var designer = await _context.Designers
                .Include(d => d.DesignerShows)
                .ThenInclude(ds => ds.Show)
                .FirstOrDefaultAsync(d => d.DesignerId == id);

            if (designer == null) return NotFound();

            return View(designer);
        }

        /// <summary>
        /// Handles the form submission to delete a designer (Admin Only).
        /// </summary>
        [HttpPost("delete/{id}")]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var designer = await _context.Designers
                .Include(d => d.DesignerShows)
                .FirstOrDefaultAsync(d => d.DesignerId == id);

            if (designer == null)
                return NotFound();

            _context.DesignerShows.RemoveRange(designer.DesignerShows);
            _context.Designers.Remove(designer);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Designer deleted successfully!";
            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// Displays the details of a designer (Accessible to all authenticated users).
        /// </summary>
        [HttpGet("details/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Details(int id)
        {
            var designer = await _context.Designers
                .Include(d => d.DesignerShows)
                .ThenInclude(ds => ds.Show)
                .FirstOrDefaultAsync(d => d.DesignerId == id);

            if (designer == null) return NotFound();

            return View(designer);
        }
    }
}