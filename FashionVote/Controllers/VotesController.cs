using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FashionVote.Data;
using FashionVote.Models;
using FashionVote.DTOs;
using System.Linq;
using System.Threading.Tasks;
using FashionVote.Models.DTOs;
using FashionVote.Hubs;
using Microsoft.AspNetCore.SignalR;
using System.IO;
using Microsoft.AspNetCore.Hosting;


namespace FashionVote.Controllers
{
    [Authorize]
    [Route("Votes")]
    public class VotesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public VotesController(ApplicationDbContext context, UserManager<IdentityUser> userManager, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _userManager = userManager;
            _webHostEnvironment = webHostEnvironment;
        }


        /// <summary>
        /// Displays the list of all votes (Admin only).
        /// </summary>
        [HttpGet("")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Index()
        {
            var votes = await _context.Votes
                .Include(v => v.Participant)
                .Include(v => v.Designer)
                .Include(v => v.Show)
                .ToListAsync();

            return View(votes);
        }


        /// <summary>
        /// Displays vote details and designer vote counts for a specific show (Admin only).
        /// </summary>
        [HttpGet("ShowVotes/{showId}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ShowVotes(int showId)
        {
            var show = await _context.Shows
                .Include(s => s.DesignerShows).ThenInclude(ds => ds.Designer)
                .Include(s => s.Votes)
                .FirstOrDefaultAsync(s => s.ShowId == showId);

            if (show == null)
            {
                TempData["ErrorMessage"] = "Show not found.";
                return RedirectToAction("Index", "Shows");
            }

            var designerVoteCounts = show.DesignerShows
                .Select(ds => new DesignerVoteDTO
                {
                    DesignerName = ds.Designer.Name,
                    Category = ds.Designer.Category,
                    VoteCount = show.Votes.Count(v => v.DesignerId == ds.Designer.DesignerId)
                })
                .OrderByDescending(dv => dv.VoteCount)
                .ToList();

            ViewBag.DesignerVotes = designerVoteCounts;
            return View(show);
        }


        /// <summary>
        /// Displays the voting page for a participant for a specific show.
        /// </summary>
        [HttpGet("Vote/{showId}")]
        [Authorize(Roles = "Participant")]
        public async Task<IActionResult> Vote(int showId)
        {
            var userEmail = User.Identity.Name;

            var participant = await _context.Participants
                .Include(p => p.ParticipantShows)
                .ThenInclude(ps => ps.Show)
                .Include(p => p.Votes)
                .FirstOrDefaultAsync(p => p.Email == userEmail);

            if (participant == null || !participant.ParticipantShows.Any(ps => ps.ShowId == showId))
            {
                TempData["ErrorMessage"] = "You are not registered for this show.";
                return RedirectToAction("MyShows", "Shows");
            }

            var show = await _context.Shows
                .Include(s => s.DesignerShows).ThenInclude(ds => ds.Designer)
                .Include(s => s.Votes)
                .FirstOrDefaultAsync(s => s.ShowId == showId);

            if (show == null)
            {
                TempData["ErrorMessage"] = "Show not found.";
                return RedirectToAction("MyShows", "Shows");
            }

            // Votes is Initialized to an Empty List if null
            show.Votes ??= new List<Vote>();

            return View(show);
        }



        /// <summary>
        /// Submits votes for one or more designers in a specific show (Participant only).
        /// </summary>
        [HttpPost("SubmitVote")]
        [Authorize(Roles = "Participant")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitVote(int showId, List<int> DesignerIds, [FromServices] IHubContext<VoteHub> hubContext)
        {
            var userEmail = User.Identity.Name;
            var participant = await _context.Participants
                .Include(p => p.ParticipantShows)
                .FirstOrDefaultAsync(p => p.Email == userEmail);

            if (participant == null || !participant.ParticipantShows.Any(ps => ps.ShowId == showId))
            {
                TempData["ErrorMessage"] = "You are not registered for this show.";
                return RedirectToAction("MyShows", "Shows");
            }

            if (DesignerIds == null || !DesignerIds.Any())
            {
                TempData["ErrorMessage"] = "You didnt vote for any designer.";
                return RedirectToAction("MyShows", "Shows");
            }

            Console.WriteLine($"Submitting vote for Show: {showId}, Participant: {participant.ParticipantId}");

            foreach (var designerId in DesignerIds)
            {
                var existingVote = await _context.Votes
                    .FirstOrDefaultAsync(v => v.ParticipantId == participant.ParticipantId && v.DesignerId == designerId && v.ShowId == showId);

                if (existingVote == null)
                {
                    Console.WriteLine($"Adding new vote: DesignerId={designerId}");
                    _context.Votes.Add(new Vote
                    {
                        ParticipantId = participant.ParticipantId,
                        DesignerId = designerId,
                        ShowId = showId,
                        VotedAt = DateTime.UtcNow,
                        ImageUrl = null
                    });
                }
            }

            await _context.SaveChangesAsync();

            // Notify all clients about vote update
            await hubContext.Clients.All.SendAsync("ReceiveVoteUpdate", showId);

            TempData["SuccessMessage"] = "Your vote has been submitted successfully! You can now upload your outfit image.";
            return RedirectToAction("Vote", new { showId });
        }



        /// <summary>
        /// Removes a vote for a designer in a specific show (Participant only).
        /// </summary>
        [HttpPost("Unvote")]
        [Authorize(Roles = "Participant")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Unvote(int showId, int designerId)
        {
            var userEmail = User.Identity.Name;
            var participant = await _context.Participants
                .Include(p => p.Votes)
                .FirstOrDefaultAsync(p => p.Email == userEmail);

            if (participant == null)
            {
                TempData["ErrorMessage"] = "Only registered participants can unvote.";
                return RedirectToAction("Vote", new { showId });
            }

            var vote = await _context.Votes
                .FirstOrDefaultAsync(v => v.ParticipantId == participant.ParticipantId &&
                                        v.ShowId == showId &&
                                        v.DesignerId == designerId);

            if (vote == null)
            {
                TempData["ErrorMessage"] = "No vote found to remove.";
                return RedirectToAction("Vote", new { showId });
            }

            Console.WriteLine($"Removing vote: DesignerId={designerId}, ShowId={showId}, ParticipantId={participant.ParticipantId}");

            // If an image is uploaded, delete the image file
            if (!string.IsNullOrEmpty(vote.ImageUrl))
            {
                string filePath = Path.Combine(_webHostEnvironment.WebRootPath, vote.ImageUrl.TrimStart('/'));
                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                }
            }

            _context.Votes.Remove(vote);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Your vote has been removed successfully! Any uploaded image has also been deleted.";
            return RedirectToAction("Vote", new { showId });
        }


        /// <summary>
        /// Handles image upload for a participant after voting.
        /// </summary>
        [HttpPost("UploadImage")]
        [Authorize(Roles = "Participant")]
        public async Task<IActionResult> UploadImage(int voteId, IFormFile imageFile)
        {
            var vote = await _context.Votes.FindAsync(voteId);
            if (vote == null)
            {
                return NotFound();
            }

            if (imageFile == null || imageFile.Length == 0)
            {
                TempData["ErrorMessage"] = "Please select a valid image.";
                return RedirectToAction("Vote", new { showId = vote.ShowId });
            }

            string uploadFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads");
            if (!Directory.Exists(uploadFolder))
            {
                Directory.CreateDirectory(uploadFolder);
            }

            // Save the image
            string fileName = $"{Guid.NewGuid()}_{imageFile.FileName}";
            string filePath = Path.Combine(uploadFolder, fileName);

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await imageFile.CopyToAsync(fileStream);
            }

            // Save image path in the database
            vote.ImageUrl = $"/uploads/{fileName}";
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Image uploaded successfully!";
            return RedirectToAction("Vote", new { showId = vote.ShowId });
        }
        

        /// <summary>
        /// Deletes an uploaded image.
        /// </summary>
        [HttpPost("DeleteImage")]
        [Authorize(Roles = "Participant")]
        public async Task<IActionResult> DeleteImage(int voteId)
        {
            var vote = await _context.Votes.FindAsync(voteId);
            if (vote == null || string.IsNullOrEmpty(vote.ImageUrl))
            {
                return NotFound();
            }

            string filePath = Path.Combine(_webHostEnvironment.WebRootPath, vote.ImageUrl.TrimStart('/'));
            if (System.IO.File.Exists(filePath))
            {
                System.IO.File.Delete(filePath);
            }

            vote.ImageUrl = null;
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Image deleted successfully!";
            return RedirectToAction("Vote", new { showId = vote.ShowId });
        }


        /// <summary>
        /// Update an uploaded image.
        /// </summary>
        [HttpPost("UpdateImage")]
        [Authorize(Roles = "Participant")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateImage(int voteId, IFormFile newImageFile)
        {
            if (newImageFile == null || newImageFile.Length == 0)
            {
                TempData["ErrorMessage"] = "Please select an image to upload.";
                return RedirectToAction("Vote", new { showId = voteId });
            }

            var vote = await _context.Votes.FindAsync(voteId);
            if (vote == null)
            {
                TempData["ErrorMessage"] = "Vote record not found.";
                return RedirectToAction("Vote", new { showId = voteId });
            }

            // Process new image upload
            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads");
            Directory.CreateDirectory(uploadsFolder);
            
            var uniqueFileName = $"{Guid.NewGuid()}_{newImageFile.FileName}";
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await newImageFile.CopyToAsync(fileStream);
            }

            // Delete the old image file if exists
            if (!string.IsNullOrEmpty(vote.ImageUrl))
            {
                var oldImagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", vote.ImageUrl.TrimStart('/'));
                if (System.IO.File.Exists(oldImagePath))
                {
                    System.IO.File.Delete(oldImagePath);
                }
            }

            // Update the vote record with the new image URL
            vote.ImageUrl = $"/uploads/{uniqueFileName}";
            _context.Update(vote);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Image updated successfully!";
            return RedirectToAction("Vote", new { showId = vote.ShowId });
        }


    }
}