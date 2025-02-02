using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FashionVote.Models
{
    public class Participant
    {
        public int ParticipantId { get; set; }

        [Required(ErrorMessage = "Name is required.")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Invalid email format.")]
        public string Email { get; set; }

        public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;

        // One-to-Many: A participant attends ONE show
        // [Required(ErrorMessage = "You must select a show.")]
        public int? ShowId { get; set; }
        public Show Show { get; set; }

        // ✅ Initialize Collections to Prevent Validation Errors
        public ICollection<Vote> Votes { get; set; } = new List<Vote>();   
    }
}
