global using Microsoft.EntityFrameworkCore;
global using System.ComponentModel.DataAnnotations;

namespace Altairis.Incitatus.Data;

public class IncitatusDbContext : DbContext {

    // Constructors

    public IncitatusDbContext(DbContextOptions options) : base(options) { }

    // Properties

    public DbSet<Site> Sites => this.Set<Site>();

    public DbSet<Page> Pages => this.Set<Page>();

    // Methods

    protected override void OnModelCreating(ModelBuilder modelBuilder) {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<Site>().Property(x => x.UpdateKey)
            .HasColumnType("nchar(30)")
            .IsFixedLength();
    }
}
