using System;
using FashionVote.Models;

namespace FashionVote.DTOs
{
  public class ShowDTO
  {
      public int ShowId { get; set; }
      public string ShowName { get; set; }
      public string Location { get; set; }
      public DateTime StartTime { get; set; }
      public DateTime EndTime { get; set; }

      public ShowDTO(Show show)
      {
          ShowId = show.ShowId;
          ShowName = show.ShowName;
          Location = show.Location;
          StartTime = show.StartTime;
          EndTime = show.EndTime;
      }
  }
}