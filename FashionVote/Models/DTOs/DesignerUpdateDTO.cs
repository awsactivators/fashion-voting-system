using System.ComponentModel.DataAnnotations;

namespace FashionVote.Models.DTOs
{
    public class DesignerUpdateDTO
    {
        [Required]
        public int DesignerId { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        [Required]
        [StringLength(50)]
        public string Category { get; set; }

        public List<int>? SelectedShowIds { get; set; }
    }
}
