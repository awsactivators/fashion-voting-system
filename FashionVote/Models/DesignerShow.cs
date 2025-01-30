namespace FashionVote.Models

{
    public class DesignerShow
    {
        public int DesignerId { get; set; }
        public Designer Designer { get; set; }

        public int ShowId { get; set; }
        public Show Show { get; set; }
    }
}
