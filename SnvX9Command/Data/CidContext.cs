using Microsoft.EntityFrameworkCore;
using SnvX9Command.Entities;

namespace SnvX9Command.Data;

public class CidContext : DbContext
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