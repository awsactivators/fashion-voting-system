using FashionVote.Models;
using System;

namespace FashionVote.DTOs
{
    public class VoteDTO
    {
        public int VoteId { get; set; }
        public int ParticipantId { get; set; }
        public string ParticipantName { get; set; }
        public int DesignerId { get; set; }
        public string DesignerName { get; set; }
        public int ShowId { get; set; }
        public string ShowName { get; set; }
        public DateTime VotedAt { get; set; }

        public VoteDTO(Vote vote)
        {
            VoteId = vote.VoteId;
            ParticipantId = vote.ParticipantId;
            ParticipantName = vote.Participant.Name;
            DesignerId = vote.DesignerId;
            DesignerName = vote.Designer.Name;
            ShowId = vote.ShowId;
            ShowName = vote.Show.ShowName;
            VotedAt = vote.VotedAt;
        }
    }

    public class VoteSubmissionDTO
    {
        public int ShowId { get; set; }
        public int[] DesignerIds { get; set; }
    }

    public class VoteRemovalDTO
    {
        public int ShowId { get; set; }
        public int DesignerId { get; set; }
    }
}
