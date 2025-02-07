using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FashionVote.Data;
using FashionVote.Models;
using FashionVote.Models.DTOs;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;


namespace FashionVote.Controllers.Api
{
    [ApiController]
    [Route("api/designers")]
    [Produces("application/json")]
    public class DesignersApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public DesignersApiController(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Retrieves a list of all designers with their assigned shows.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetDesigners()
        {
            var designers = await _context.Designers
                .Include(d => d.DesignerShows)
                .ThenInclude(ds => ds.Show)
                .ToListAsync();

            return Ok(designers.Select(d => new DesignerDTO(d)));
        }

        /// <summary>
        /// Retrieves details of a specific designer.
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetDesignerDetails(int id)
        {
            var designer = await _context.Designers
                .Include(d => d.DesignerShows)
                .ThenInclude(ds => ds.Show)
                .FirstOrDefaultAsync(d => d.DesignerId == id);

            if (designer == null)
                return NotFound(new { message = "Designer not found." });

            return Ok(new DesignerDTO(designer));
        }

        /// <summary>
        /// Creates a new designer via API.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CreateDesigner([FromBody] DesignerCreateDTO designerDto)
        {
            if (designerDto == null)
                return BadRequest(new { message = "Invalid data. Designer data is required." });

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

            return CreatedAtAction(nameof(GetDesignerDetails), new { id = designer.DesignerId }, new { message = "Designer created successfully!", designerId = designer.DesignerId });
        }

        /// <summary>
        /// Updates an existing designer via API.
        /// </summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateDesigner(int id, [FromBody] DesignerUpdateDTO designerDto)
        {
            if (id != designerDto.DesignerId) 
                return BadRequest(new { message = "ID mismatch." });

            var designer = await _context.Designers
                .Include(d => d.DesignerShows)
                .FirstOrDefaultAsync(d => d.DesignerId == id);

            if (designer == null)
                return NotFound(new { message = "Designer not found." });

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

            return Ok(new { message = "Designer updated successfully!" });
        }

        /// <summary>
        /// Deletes a designer via API.
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteDesigner(int id)
        {
            var designer = await _context.Designers
                .Include(d => d.DesignerShows)
                .FirstOrDefaultAsync(d => d.DesignerId == id);

            if (designer == null)
                return NotFound(new { message = "Designer not found." });

            _context.DesignerShows.RemoveRange(designer.DesignerShows);
            _context.Designers.Remove(designer);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Designer deleted successfully." });
        }
    }
}
