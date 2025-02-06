using System;

namespace FashionVote.Models.DTOs
{
    public class ShowDto
    {
        public int ShowId { get; set; }
        public string ShowName { get; set; }
        public string Location { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }

        // Constructor to map Show entity to DTO
        public ShowDto(Show show)
        {
            ShowId = show.ShowId;
            ShowName = show.ShowName;
            Location = show.Location;
            StartTime = show.StartTime;
            EndTime = show.EndTime;
        }
    }
}
