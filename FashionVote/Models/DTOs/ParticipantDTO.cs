using System;
using FashionVote.Models;

namespace FashionVote.DTOs
{
  public class ParticipantDTO
  {
      public int ParticipantId { get; set; }
      public string Name { get; set; }
      public string Email { get; set; }
      public List<ShowDTO> RegisteredShows { get; set; }

      public ParticipantDTO(Participant participant)
      {
          ParticipantId = participant.ParticipantId;
          Name = participant.Name;
          Email = participant.Email;
          RegisteredShows = participant.ParticipantShows?
              .Select(ps => new ShowDTO(ps.Show))
              .ToList();
      }
  }
}