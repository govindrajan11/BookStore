using BookStore.Domain;
namespace BookStore.Application;
public record CatalogBook(string OpenLibraryKey,string Title,string Author,string? Isbn,int? PublishedYear,string? CoverUrl);
public record PageResult<T>(IReadOnlyList<T> Items,int Total,int Page,int PageSize);
public record AddBookRequest(string OpenLibraryKey,string Title,string Author,string? Isbn,int? PublishedYear,string? CoverUrl,string? Description,Genre Genre,decimal Price,int Stock);
public record UpdateBookRequest(Genre Genre,decimal Price,int Stock,string? Description);
public interface IBookStore {
 Task<PageResult<Book>> List(Genre? genre,int page,int pageSize,string sort,bool desc,CancellationToken ct);
 Task<Book?> Find(Guid id,CancellationToken ct);
 Task<bool> Exists(string key,string? isbn,CancellationToken ct);
 Task Add(Book book,CancellationToken ct); Task Save(CancellationToken ct);
 void Delete(Book book);
 Task<IReadOnlyList<GenreCount>> Counts(CancellationToken ct);
}
public record GenreCount(Genre Genre,int Count);
public interface ICatalog { Task<IReadOnlyList<CatalogBook>> Search(string query,CancellationToken ct); }
public interface IRequest<T> {}
public interface IHandler<TRequest,TResponse> where TRequest:IRequest<TResponse> { Task<TResponse> Handle(TRequest request,CancellationToken ct); }
public interface IMediator { Task<TResponse> Send<TResponse>(IRequest<TResponse> request,CancellationToken ct=default); }
public sealed class Mediator(IServiceProvider services):IMediator {
 public Task<TResponse> Send<TResponse>(IRequest<TResponse> request,CancellationToken ct=default) {
  var type=typeof(IHandler<,>).MakeGenericType(request.GetType(),typeof(TResponse));
  var handler=services.GetService(type) ?? throw new InvalidOperationException($"No handler for {request.GetType().Name}");
  return (Task<TResponse>)type.GetMethod("Handle")!.Invoke(handler,new object[]{request,ct})!;
 }
}
public sealed class AppException(int status,string message):Exception(message) { public int Status {get;}=status; }
public static class BookRules {
 public static void Validate(Genre genre,decimal price,int stock) {
  if(!Enum.IsDefined(genre)) throw new AppException(400,"Invalid genre");
  if(price<0 || price>1000000) throw new AppException(400,"Price must be between 0 and 1000000");
  if(stock<0) throw new AppException(400,"Stock cannot be negative");
 }
}
