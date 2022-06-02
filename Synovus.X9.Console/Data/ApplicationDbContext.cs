using Microsoft.EntityFrameworkCore;

namespace Synovus.X9.Console.Data;

public class ApplicationDbContext: DbContext
{
    public DbSet<Customer> Customers { get; set; }
    
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseMySQL("server=localhost;database=cid;user=root;password=1Password"); 
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<Customer>().ToTable("confirms"); 
        
        modelBuilder.Entity<Customer>(e =>
        {
            e.HasKey(customer => customer.AutoIndex);
        }); 
        
    }
}