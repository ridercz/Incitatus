global using System.ComponentModel.DataAnnotations;
global using Microsoft.EntityFrameworkCore;

namespace Altairis.Incitatus.Data;

public class IncitatusDbContext : DbContext {

    // Constructors

    public IncitatusDbContext(DbContextOptions options) : base(options) { }

    // Properties

    public DbSet<Site> Sites => this.Set<Site>();

    public DbSet<Page> Pages => this.Set<Page>();

    // Methods

    public record SearchResult(int Rank, string Url, string Title, string Description, DateTime DateLastUpdated);

    public IQueryable<SearchResult> SearchPages(Guid siteId, string query)
        => this.FromExpression(() => SearchPages(siteId, SqlServerQueryTranslator.ToSqlQuery(query)).OrderByDescending(x => x.Rank));

    protected override void OnModelCreating(ModelBuilder modelBuilder) {
        base.OnModelCreating(modelBuilder);

        // Setup update key as nchar instead of nvarchar
        modelBuilder.Entity<Site>().Property(x => x.UpdateKey)
            .HasColumnType("nchar(30)")
            .IsFixedLength();

        // Map method to TVF
        modelBuilder.Entity<SearchResult>().ToTable((string?)null).HasNoKey();
        var searchPagesMethodInfo = typeof(IncitatusDbContext).GetMethod(nameof(SearchPages), new[] { typeof(Guid), typeof(string) }) ?? throw new Exception("The SearchPages method was not found.");
        modelBuilder.HasDbFunction(searchPagesMethodInfo).HasName("SearchPagesInSite");
    }

}
