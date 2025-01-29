namespace FashionVote.Models 
{
  public class Show
  {
    public int ShowId { get; set; }
    public string ShowName { get; set; }
    public string Location { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    // public string Description { get; set; }
    public ICollection<ParticipantShow> ParticipantShows { get; set; }
    public ICollection<DesignerShow> DesignerShows { get; set; }
  }

}
