using System;
using FashionVote.Models;

namespace FashionVote.DTOs
{
    public class DesignerUpdateDTO
    {
        public int DesignerId { get; set; } // Required for updates
        public string Name { get; set; }
        public string Category { get; set; }
        public int[] SelectedShowIds { get; set; }
    }

}