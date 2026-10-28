
using BookStore.Domain;

namespace BookStore.Application.Features.Books;

public record GetBooks(
    Genre? Genre,
    int Page = 1,
    int PageSize = 10,
    string Sort = "title",
    bool Desc = false
) : IRequest<PageResult<Book>>;

public sealed class GetBooksHandler(IBookStore store) : IHandler<GetBooks, PageResult<Book>>
{
    public Task<PageResult<Book>> Handle(GetBooks q, CancellationToken ct) =>
        store.List(
            q.Genre,
            Math.Max(1, q.Page),
            Math.Clamp(q.PageSize, 1, 100),
            q.Sort,
            q.Desc,
            ct
        );
}

public record GetBook(Guid Id) : IRequest<Book>;

public sealed class GetBookHandler(IBookStore store) : IHandler<GetBook, Book>
{
    public async Task<Book> Handle(GetBook q, CancellationToken ct) =>
        await store.Find(q.Id, ct) ?? throw new AppException(404, "Book not found");
}

public record AddBook(AddBookRequest Data) : IRequest<Book>;

public sealed class AddBookHandler(IBookStore store) : IHandler<AddBook, Book>
{
    public async Task<Book> Handle(AddBook q, CancellationToken ct)
    {
        var d = q.Data;

        BookRules.Validate(d.Genre, d.Price, d.Stock);

        if (string.IsNullOrWhiteSpace(d.Title) ||
            string.IsNullOrWhiteSpace(d.OpenLibraryKey) ||
            !d.OpenLibraryKey.StartsWith("/works/"))
            throw new AppException(400, "A valid Open Library work must be selected");

        if (await store.Exists(d.OpenLibraryKey, d.Isbn, ct))
            throw new AppException(409, "Book already exists");

        var book = new Book
        {
            OpenLibraryKey = d.OpenLibraryKey.Trim(),
            Title = d.Title.Trim(),
            Author = d.Author.Trim(),
            Isbn = d.Isbn,
            PublishedYear = d.PublishedYear,
            CoverUrl = d.CoverUrl,
            Description = d.Description,
            Genre = d.Genre,
            Price = d.Price,
            Stock = d.Stock
        };

        await store.Add(book, ct);
        await store.Save(ct);

        return book;
    }
}

public record UpdateBook(Guid Id, UpdateBookRequest Data) : IRequest<Book>;

public sealed class UpdateBookHandler(IBookStore store) : IHandler<UpdateBook, Book>
{
    public async Task<Book> Handle(UpdateBook q, CancellationToken ct)
    {
        BookRules.Validate(q.Data.Genre, q.Data.Price, q.Data.Stock);

        var book = await store.Find(q.Id, ct) ?? throw new AppException(404, "Book not found");

        book.Genre = q.Data.Genre;
        book.Price = q.Data.Price;
        book.Stock = q.Data.Stock;
        book.Description = q.Data.Description;

        await store.Save(ct);

        return book;
    }
}

public record DeleteBook(Guid Id) : IRequest<bool>;

public sealed class DeleteBookHandler(IBookStore store) : IHandler<DeleteBook, bool>
{
    public async Task<bool> Handle(DeleteBook q, CancellationToken ct)
    {
        var book = await store.Find(q.Id, ct) ?? throw new AppException(404, "Book not found");

        store.Delete(book);
        await store.Save(ct);

        return true;
    }
}

public record GetGenreCounts() : IRequest<IReadOnlyList<GenreCount>>;

public sealed class GetGenreCountsHandler(IBookStore store) : IHandler<GetGenreCounts, IReadOnlyList<GenreCount>>
{
    public Task<IReadOnlyList<GenreCount>> Handle(GetGenreCounts q, CancellationToken ct) =>
        store.Counts(ct);
}
