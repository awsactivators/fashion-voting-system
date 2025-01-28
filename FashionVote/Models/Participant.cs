namespace FashionVote.Models 
{
  public class Participant
  {
    public int ParticipantId { get; set; }
    public string Name { get; set; }
    public string Email { get; set; }
    public DateTime RegisteredAt { get; set; }
    public ICollection<ParticipantShow> ParticipantShows { get; set; }
  }

}
