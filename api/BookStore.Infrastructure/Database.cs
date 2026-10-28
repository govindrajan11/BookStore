
using BookStore.Application;
using BookStore.Domain;
using Microsoft.EntityFrameworkCore;

namespace BookStore.Infrastructure;

public sealed class BookDb(DbContextOptions<BookDb> options) : DbContext(options)
{
    public DbSet<Book> Books => Set<Book>();

    protected override void OnModelCreating(ModelBuilder model)
    {
        var b = model.Entity<Book>();

        b.HasKey(x => x.Id);
        b.HasIndex(x => x.OpenLibraryKey).IsUnique();

        b.Property(x => x.Genre).HasConversion<string>();
        b.Property(x => x.Price).HasConversion<double>();
    }
}

public sealed class SqliteBookStore(BookDb db) : IBookStore
{
    public async Task<PageResult<Book>> List(
        Genre? genre,
        int page,
        int pageSize,
        string sort,
        bool desc,
        CancellationToken ct)
    {
        IQueryable<Book> q = db.Books.AsNoTracking();

        if (genre.HasValue)
            q = q.Where(b => b.Genre == genre.Value);

        int total = await q.CountAsync(ct);

        q = (sort.ToLowerInvariant(), desc) switch
        {
            ("price", false) => q.OrderBy(b => b.Price).ThenBy(b => b.Id),
            ("price", true) => q.OrderByDescending(b => b.Price).ThenBy(b => b.Id),

            ("stock", false) => q.OrderBy(b => b.Stock).ThenBy(b => b.Id),
            ("stock", true) => q.OrderByDescending(b => b.Stock).ThenBy(b => b.Id),

            ("author", false) => q.OrderBy(b => b.Author).ThenBy(b => b.Id),
            ("author", true) => q.OrderByDescending(b => b.Author).ThenBy(b => b.Id),

            ("title", true) => q.OrderByDescending(b => b.Title).ThenBy(b => b.Id),

            _ => q.OrderBy(b => b.Title).ThenBy(b => b.Id)
        };

        return new PageResult<Book>(
            await q.Skip((page - 1) * pageSize)
                   .Take(pageSize)
                   .ToListAsync(ct),
            total,
            page,
            pageSize
        );
    }

    public Task<Book?> Find(Guid id, CancellationToken ct) =>
        db.Books.FirstOrDefaultAsync(b => b.Id == id, ct);

    public Task<bool> Exists(string key, string? isbn, CancellationToken ct) =>
        db.Books.AnyAsync(
            b => b.OpenLibraryKey == key ||
                 (isbn != null && isbn != "" && b.Isbn == isbn),
            ct
        );

    public async Task Add(Book b, CancellationToken ct) =>
        await db.Books.AddAsync(b, ct);

    public Task Save(CancellationToken ct) =>
        db.SaveChangesAsync(ct);

    public void Delete(Book b) =>
        db.Books.Remove(b);

    public async Task<IReadOnlyList<GenreCount>> Counts(CancellationToken ct) =>
        await db.Books.AsNoTracking()
                      .GroupBy(b => b.Genre)
                      .Select(g => new GenreCount(g.Key, g.Count()))
                      .ToListAsync(ct);
}
