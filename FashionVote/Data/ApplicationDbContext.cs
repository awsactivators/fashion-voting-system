using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using FashionVote.Models;
using Microsoft.AspNetCore.Identity;

namespace FashionVote.Data
{
    public class ApplicationDbContext : IdentityDbContext<IdentityUser, IdentityRole, string>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        { }

        // Tables
        public DbSet<Participant> Participants { get; set; }
        public DbSet<Designer> Designers { get; set; }
        public DbSet<Show> Shows { get; set; }
        public DbSet<DesignerShow> DesignerShows { get; set; }
        public DbSet<Vote> Votes { get; set; } 
        public DbSet<ParticipantShow> ParticipantShows { get; set; } 


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder); // Ensure Identity configurations are applied

            // Configure Many-to-Many: Designer & Show
            modelBuilder.Entity<DesignerShow>()
                .HasKey(ds => new { ds.DesignerId, ds.ShowId });

            modelBuilder.Entity<DesignerShow>()
                .HasOne(ds => ds.Designer)
                .WithMany(d => d.DesignerShows)
                .HasForeignKey(ds => ds.DesignerId);

            modelBuilder.Entity<DesignerShow>()
                .HasOne(ds => ds.Show)
                .WithMany(s => s.DesignerShows)
                .HasForeignKey(ds => ds.ShowId);

            // Configure Many-to-Many: Votes (Participant votes for Designers in a Show)
            modelBuilder.Entity<Vote>()
                .HasKey(v => v.VoteId);

            modelBuilder.Entity<Vote>()
                .HasOne(v => v.Participant)
                .WithMany(p => p.Votes)
                .HasForeignKey(v => v.ParticipantId);

            modelBuilder.Entity<Vote>()
                .HasOne(v => v.Designer)
                .WithMany(d => d.Votes)
                .HasForeignKey(v => v.DesignerId);

            modelBuilder.Entity<Vote>()
                .HasOne(v => v.Show)
                .WithMany(s => s.Votes)
                .HasForeignKey(v => v.ShowId);
            
            // Prevent duplicate votes: One vote per participant-designer-show
            modelBuilder.Entity<Vote>()
                .HasIndex(v => new { v.ParticipantId, v.DesignerId, v.ShowId })
                .IsUnique();

            // ✅ Many-to-Many Relationship for Participant and Shows
            modelBuilder.Entity<ParticipantShow>()
                .HasKey(ps => new { ps.ParticipantId, ps.ShowId });

            modelBuilder.Entity<ParticipantShow>()
                .HasOne(ps => ps.Participant)
                .WithMany(p => p.ParticipantShows)
                .HasForeignKey(ps => ps.ParticipantId);

            modelBuilder.Entity<ParticipantShow>()
                .HasOne(ps => ps.Show)
                .WithMany(s => s.ParticipantShows)
                .HasForeignKey(ps => ps.ShowId);
        }
    }
}
