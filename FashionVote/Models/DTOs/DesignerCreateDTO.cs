using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

namespace FashionVote.Models.DTOs
{
    /// <summary>
    /// DTO for creating a new designer.
    /// </summary>
    public class DesignerCreateDTO
    {
        [Required(ErrorMessage = "Designer name is required.")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Category is required.")]
        public string Category { get; set; }

        /// <summary>
        /// List of selected show IDs the designer is assigned to.
        /// </summary>
        // public int[] SelectedShowIds { get; set; } = new int[0];
        public List<int> SelectedShowIds { get; set; } = new List<int>();

    }
}
