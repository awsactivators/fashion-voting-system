namespace FashionVote.Models
{
  public class Designer
  {
    public int DesignerId { get; set; }
    public string Name { get; set; }
    public string Category { get; set; }
    // public DateTime CreatedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<DesignerShow> DesignerShows { get; set; }
  }
}

