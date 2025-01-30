using System;

namespace FashionVote.Models
{
    public class Vote
    {
        public int VoteId { get; set; }

        // Foreign Key: The participant casting the vote
        public int ParticipantId { get; set; }
        public Participant Participant { get; set; }

        // Foreign Key: The designer being voted for
        public int DesignerId { get; set; }
        public Designer Designer { get; set; }

        // Foreign Key: The show in which the vote is cast
        public int ShowId { get; set; }
        public Show Show { get; set; }

        public DateTime VotedAt { get; set; } = DateTime.UtcNow;
    }
}
