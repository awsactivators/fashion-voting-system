using System.Collections.Generic;

namespace FashionVote.Models.DTOs
{
    public class ParticipantDTO
    {
        public int ParticipantId { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public List<ShowDto> Shows { get; set; } = new List<ShowDto>();

        public ParticipantDTO(Participant participant)
        {
            ParticipantId = participant.ParticipantId;
            Name = participant.Name;
            Email = participant.Email;
            Shows = participant.ParticipantShows != null
                ? participant.ParticipantShows.Select(ps => new ShowDto(ps.Show)).ToList()
                : new List<ShowDto>();
        }
    }
}
