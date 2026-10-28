using BookStore.Application;
using BookStore.Application.Features.Books;
using BookStore.Domain;
using Xunit;

namespace BookStore.Tests;

public sealed class BookHandlersTests
{
    private static AddBookRequest ValidBook() => new(
        "/works/OL123W", "Test Book", "Test Author", "9781234567890", 2020,
        null, null, Genre.Fantasy, 19.99m, 10);

    [Fact]
    public async Task AddBook_PersistsSelectedCatalogBook()
    {
        var store = new FakeBookStore();
        var book = await new AddBookHandler(store).Handle(new AddBook(ValidBook()), default);
        Assert.Equal("Test Book", book.Title);
        Assert.Equal(Genre.Fantasy, book.Genre);
        Assert.Equal(19.99m, book.Price);
        Assert.Single(store.Books);
        Assert.Equal(1, store.SaveCalls);
    }

    [Fact]
    public async Task AddBook_DuplicateKey_ThrowsConflictAndDoesNotSave()
    {
        var store = new FakeBookStore();
        store.Books.Add(new Book { OpenLibraryKey = "/works/OL123W", Title = "Existing" });
        var error = await Assert.ThrowsAsync<AppException>(() =>
            new AddBookHandler(store).Handle(new AddBook(ValidBook()), default));
        Assert.Equal(409, error.Status);
        Assert.Single(store.Books);
        Assert.Equal(0, store.SaveCalls);
    }

    [Theory]
    [InlineData(-1, 0)]
    [InlineData(1, -1)]
    [InlineData(1000001, 0)]
    public async Task AddBook_InvalidPriceOrStock_ReturnsBadRequest(int price, int stock)
    {
        var store = new FakeBookStore();
        var error = await Assert.ThrowsAsync<AppException>(() =>
            new AddBookHandler(store).Handle(new AddBook(ValidBook() with
                { Price = price, Stock = stock }), default));
        Assert.Equal(400, error.Status);
        Assert.Empty(store.Books);
    }

    [Fact]
    public async Task GetBooks_FiltersGenreAndNormalizesPagination()
    {
        var store = new FakeBookStore();
        store.Books.AddRange([
            new Book { Title = "Fantasy", Genre = Genre.Fantasy },
            new Book { Title = "History", Genre = Genre.History }
        ]);
        var result = await new GetBooksHandler(store).Handle(
            new GetBooks(Genre.Fantasy, 0, 999), default);
        Assert.Single(result.Items);
        Assert.Equal("Fantasy", result.Items[0].Title);
        Assert.Equal(1, result.Page);
        Assert.Equal(100, result.PageSize);
    }

    [Fact]
    public async Task UpdateBook_ChangesOnlyStoreOwnedFields()
    {
        var store = new FakeBookStore();
        var existing = new Book { Title = "Original", OpenLibraryKey = "/works/OL1W", Price = 5m };
        store.Books.Add(existing);
        var result = await new UpdateBookHandler(store).Handle(new UpdateBook(
            existing.Id, new UpdateBookRequest(Genre.Mystery, 25m, 7, "Updated")), default);
        Assert.Equal("Original", result.Title);
        Assert.Equal("/works/OL1W", result.OpenLibraryKey);
        Assert.Equal(25m, result.Price);
        Assert.Equal(7, result.Stock);
        Assert.Equal(Genre.Mystery, result.Genre);
        Assert.Equal(1, store.SaveCalls);
    }

    [Fact]
    public async Task DeleteBook_RemovesBook()
    {
        var store = new FakeBookStore();
        var existing = new Book { Title = "Delete me" };
        store.Books.Add(existing);
        Assert.True(await new DeleteBookHandler(store).Handle(new DeleteBook(existing.Id), default));
        Assert.Empty(store.Books);
        Assert.Equal(1, store.SaveCalls);
    }

    [Fact]
    public async Task GetBook_MissingId_ReturnsNotFound()
    {
        var error = await Assert.ThrowsAsync<AppException>(() =>
            new GetBookHandler(new FakeBookStore()).Handle(new GetBook(Guid.NewGuid()), default));
        Assert.Equal(404, error.Status);
    }
}

internal sealed class FakeBookStore : IBookStore
{
    public List<Book> Books { get; } = [];
    public int SaveCalls { get; private set; }

    public Task<PageResult<Book>> List(Genre? genre, int page, int pageSize, string sort, bool desc, CancellationToken ct)
    {
        var query = Books.Where(b => genre is null || b.Genre == genre.Value);
        query = sort.ToLowerInvariant() switch
        {
            "price" => desc ? query.OrderByDescending(b => b.Price) : query.OrderBy(b => b.Price),
            _ => desc ? query.OrderByDescending(b => b.Title) : query.OrderBy(b => b.Title)
        };
        var all = query.ToList();
        return Task.FromResult(new PageResult<Book>(all.Skip((page - 1) * pageSize).Take(pageSize).ToList(), all.Count, page, pageSize));
    }

    public Task<Book?> Find(Guid id, CancellationToken ct) => Task.FromResult(Books.FirstOrDefault(b => b.Id == id));
    public Task<bool> Exists(string key, string? isbn, CancellationToken ct) => Task.FromResult(
        Books.Any(b => b.OpenLibraryKey == key || (isbn is not null && b.Isbn == isbn)));
    public Task Add(Book book, CancellationToken ct) { Books.Add(book); return Task.CompletedTask; }
    public Task Save(CancellationToken ct) { SaveCalls++; return Task.CompletedTask; }
    public void Delete(Book book) => Books.Remove(book);
    public Task<IReadOnlyList<GenreCount>> Counts(CancellationToken ct) => Task.FromResult<IReadOnlyList<GenreCount>>(
        Books.GroupBy(b => b.Genre).Select(g => new GenreCount(g.Key, g.Count())).ToList());
}
