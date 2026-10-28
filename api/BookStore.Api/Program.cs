using BookStore.Application;
using BookStore.Application.Features.Books;
using BookStore.Application.Features.Catalog;
using BookStore.Domain;
using BookStore.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Serilog;
using System.Text.Json.Serialization;
var builder=WebApplication.CreateBuilder(args);
builder.Host.UseSerilog((ctx,config)=>config.WriteTo.Console().Enrich.FromLogContext());
builder.Services.ConfigureHttpJsonOptions(o=>o.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddDbContext<BookDb>(o=>o.UseSqlite(builder.Configuration.GetConnectionString("Books")??"Data Source=books.db"));
builder.Services.AddScoped<IBookStore,SqliteBookStore>();builder.Services.AddScoped<IMediator,Mediator>();
builder.Services.AddScoped<IHandler<GetBooks,PageResult<Book>>,GetBooksHandler>();
builder.Services.AddScoped<IHandler<GetBook,Book>,GetBookHandler>();
builder.Services.AddScoped<IHandler<AddBook,Book>,AddBookHandler>();
builder.Services.AddScoped<IHandler<UpdateBook,Book>,UpdateBookHandler>();
builder.Services.AddScoped<IHandler<DeleteBook,bool>,DeleteBookHandler>();
builder.Services.AddScoped<IHandler<GetGenreCounts,IReadOnlyList<GenreCount>>,GetGenreCountsHandler>();
builder.Services.AddScoped<IHandler<SearchCatalog,IReadOnlyList<CatalogBook>>,SearchCatalogHandler>();
builder.Services.AddHttpClient<ICatalog,OpenLibraryCatalog>(client=>{client.BaseAddress=new Uri("https://openlibrary.org/");client.Timeout=TimeSpan.FromSeconds(8);client.DefaultRequestHeaders.UserAgent.ParseAdd("BookStoreAssessment/1.0 (educational project)");});
builder.Services.AddCors(o=>o.AddPolicy("web",p=>p.WithOrigins("http://localhost:4200").AllowAnyHeader().AllowAnyMethod()));
var app=builder.Build();app.UseSerilogRequestLogging();app.UseCors("web");
app.Use(async(context,next)=>{var id=context.Request.Headers["X-Correlation-ID"].FirstOrDefault()??Guid.NewGuid().ToString("N");context.Response.Headers["X-Correlation-ID"]=id;using(Serilog.Context.LogContext.PushProperty("CorrelationId",id)) {try{await next();}catch(AppException ex){context.Response.StatusCode=ex.Status;await context.Response.WriteAsJsonAsync(new {error=ex.Message});}catch(DbUpdateException ex){app.Logger.LogWarning(ex,"Database write failed");context.Response.StatusCode=409;await context.Response.WriteAsJsonAsync(new {error="Book conflicts with an existing record"});}catch(Exception ex){app.Logger.LogError(ex,"Unexpected request error");context.Response.StatusCode=500;await context.Response.WriteAsJsonAsync(new {error="Unexpected server error"});}}});
app.MapGet("/api/catalog/search",async(string q,IMediator mediator,CancellationToken ct)=>Results.Ok(await mediator.Send(new SearchCatalog(q),ct)));
app.MapGet("/api/books",async(Genre? genre,int? page,int? pageSize,string? sort,bool? desc,IMediator mediator,CancellationToken ct)=>Results.Ok(await mediator.Send(new GetBooks(genre,page??1,pageSize??10,sort??"title",desc??false),ct)));
app.MapGet("/api/books/genres",async(IMediator mediator,CancellationToken ct)=>Results.Ok(await mediator.Send(new GetGenreCounts(),ct)));
app.MapGet("/api/books/{id:guid}",async(Guid id,IMediator mediator,CancellationToken ct)=>Results.Ok(await mediator.Send(new GetBook(id),ct)));
app.MapPost("/api/books",async(AddBookRequest data,IMediator mediator,CancellationToken ct)=>{var book=await mediator.Send(new AddBook(data),ct);return Results.Created($"/api/books/{book.Id}",book);});
app.MapPut("/api/books/{id:guid}",async(Guid id,UpdateBookRequest data,IMediator mediator,CancellationToken ct)=>Results.Ok(await mediator.Send(new UpdateBook(id,data),ct)));
app.MapDelete("/api/books/{id:guid}",async(Guid id,IMediator mediator,CancellationToken ct)=>{await mediator.Send(new DeleteBook(id),ct);return Results.NoContent();});
using(var scope=app.Services.CreateScope()) {var db=scope.ServiceProvider.GetRequiredService<BookDb>();await db.Database.EnsureCreatedAsync();if(!await db.Books.AnyAsync()){db.Books.AddRange(
 new Book{Title="Pride and Prejudice",Author="Jane Austen",OpenLibraryKey="/works/OL66554W",Genre=Genre.Fiction,Price=12.99m,Stock=7},
 new Book{Title="The Time Machine",Author="H. G. Wells",OpenLibraryKey="/works/OL52267W",Genre=Genre.SciFi,Price=9.99m,Stock=4},
 new Book{Title="The Hobbit",Author="J. R. R. Tolkien",OpenLibraryKey="/works/OL262758W",Genre=Genre.Fantasy,Price=15.50m,Stock=6});await db.SaveChangesAsync();}}
app.Run();
public partial class Program {}
