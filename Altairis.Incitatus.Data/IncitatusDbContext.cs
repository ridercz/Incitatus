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

    public IQueryable<Page> SearchStories(string query) {
        var ftxQuery = SqlServerQueryTranslator.ToSqlQuery(query);
        return this.Set<Page>().FromSqlInterpolated($"SELECT TOP 100 PERCENT S.* FROM Pages AS S INNER JOIN CONTAINSTABLE(Pages, *, {ftxQuery}) AS R ON R.[KEY] = S.Id ORDER BY R.RANK DESC");
    }

    public IQueryable<Page> SearchStories(Guid siteId, string query) {
        var ftxQuery = SqlServerQueryTranslator.ToSqlQuery(query);
        return this.Set<Page>().FromSqlInterpolated($"SELECT TOP 100 PERCENT S.* FROM Pages AS S INNER JOIN CONTAINSTABLE(Pages, *, {ftxQuery}) AS R ON R.[KEY] = S.Id WHERE S.SiteId = {siteId} ORDER BY R.RANK DESC");
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder) {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<Site>().Property(x => x.UpdateKey)
            .HasColumnType("nchar(30)")
            .IsFixedLength();
    }
}
