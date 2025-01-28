using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using FashionVote.Models;

namespace FashionVote.Data
{
    public class ApplicationDbContext : IdentityDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        { }

        public DbSet<Participant> Participants { get; set; }
        public DbSet<Designer> Designers { get; set; }
        public DbSet<Show> Shows { get; set; }
        public DbSet<ParticipantShow> ParticipantShows { get; set; }
        public DbSet<DesignerShow> DesignerShows { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder); // Ensure Identity configurations are applied

            modelBuilder.Entity<ParticipantShow>()
                .HasKey(ps => new { ps.ParticipantId, ps.ShowId });

            modelBuilder.Entity<DesignerShow>()
                .HasKey(ds => new { ds.DesignerId, ds.ShowId });
        }
    }
}
