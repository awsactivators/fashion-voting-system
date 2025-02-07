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
        /// <returns>Returns a JSON list of designers.</returns>
        /// <response code="200">Returns the list of designers.</response>
        /// <example>
        /// curl -X GET http://localhost:5157/api/designers -H "Accept: application/json"
        /// <output>
        /// [{"designerId":1,"name":"Kelvin klein","category":"Casual","shows":["Spring SS2"]},{"designerId":2,"name":"Ella MIA","category":"Traditional","shows":["Spring SS2"]}
        /// </output>
        /// </example>
        
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
        /// <param name="id">The ID of the designer to retrieve.</param>
        /// <returns>Returns a JSON object containing designer details.</returns>
        /// <response code="200">Returns the designer details.</response>
        /// <response code="404">Designer not found.</response>
        /// <example>
        /// curl -X GET http://localhost:5157/api/designers/1 -H "Accept: application/json"
        /// <output>{"designerId":1,"name":"Kelvin klein","category":"Casual","shows":["Spring SS2"]}</output>
        /// </example>
        
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
        /// <param name="designerDto">The designer data transfer object.</param>
        /// <returns>Returns the created designer ID and a success message.</returns>
        /// <response code="201">Designer created successfully.</response>
        /// <response code="400">Invalid request data.</response>
        /// <example>
        /// curl -X POST http://localhost:5157/api/designers \
            // -H "Content-Type: application/json" \
            // -d '{
            //       "name": "Versace",
            //       "category": "Streetwear",
            //       "selectedShowIds": [1, 2]
            //     }'
        /// <output>{"message":"Designer created successfully!","designerId":10}</output>
        /// </example>
        
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



        /// </summary>
        /// <param name="id">The ID of the designer to update.</param>
        /// <param name="designerDto">Updated designer data.</param>
        /// <returns>Returns a success message.</returns>
        /// <response code="200">Designer updated successfully.</response>
        /// <response code="400">ID mismatch or invalid request.</response>
        /// <response code="404">Designer not found.</response>
        /// <example>
        /// curl -X PUT http://localhost:5157/api/designers/10 \
            //  -H "Content-Type: application/json" \
            //  -d '{
            //        "designerId": 10,
            //        "name": "Donatella Versace",
            //        "category": "High Fashion",
            //        "selectedShowIds": [2, 3]
        /// <output>{"message":"Designer updated successfully!","designerId":10}</output>
        /// </example>
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
        /// <param name="id">The ID of the designer to delete.</param>
        /// <returns>Returns a success message.</returns>
        /// <response code="200">Designer deleted successfully.</response>
        /// <response code="404">Designer not found.</response>
        /// <example>
        /// curl -X DELETE http://localhost:5157/api/designers/10
        /// <output>{"message":"Designer deleted successfully."}</output>
        /// </example>
        
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
