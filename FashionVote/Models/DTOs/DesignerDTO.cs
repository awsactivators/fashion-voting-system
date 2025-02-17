using System.Collections.Generic;
using FashionVote.Models.DTOs;

namespace FashionVote.Models
{
    public class DesignerDTO
    {
        public int DesignerId { get; set; }
        public string Name { get; set; }
        public string Category { get; set; }
        public List<ShowDto> Shows { get; set; }


        public DesignerDTO(Designer designer)
        {
            DesignerId = designer.DesignerId;
            Name = designer.Name;
            Category = designer.Category;
            Shows = designer.DesignerShows?.Select(ds => new ShowDto(ds.Show)).ToList();
        }
    }
}
