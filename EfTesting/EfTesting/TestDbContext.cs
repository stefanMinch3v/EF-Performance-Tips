namespace EfTesting
{
    using Microsoft.EntityFrameworkCore;

    public class TestDbContext : DbContext
    {
        public DbSet<Cat> Cats { get; set; }

        public DbSet<Owner> Owners { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer("Server=asus-pc\\sql2016;Database=TestingEfCore;Trusted_Connection=True;MultipleActiveResultSets=true");
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder
                .Entity<Owner>()
                .HasMany(o => o.Cats)
                .WithOne(c => c.Owner)
                .HasForeignKey(c => c.OwnerId);
        }
    }
}
