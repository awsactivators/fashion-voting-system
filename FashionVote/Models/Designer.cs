using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FashionVote.Models
{
    public class Designer
    {
        public int DesignerId { get; set; }

        [Required(ErrorMessage = "Designer name is required.")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Category is required.")]
        public string Category { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Initialize Collections to Prevent Validation Errors
        public ICollection<DesignerShow> DesignerShows { get; set; } = new List<DesignerShow>();
        public ICollection<Vote> Votes { get; set; } = new List<Vote>();
    }
}
