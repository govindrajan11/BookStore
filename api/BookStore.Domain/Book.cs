namespace BookStore.Domain;
public enum Genre { Fiction, SciFi, Fantasy, Mystery, Biography, NonFiction, Children, History }
public sealed class Book {
 public Guid Id {get;set;} = Guid.NewGuid();
 public string Title {get;set;} = ""; public string Author {get;set;} = "";
 public string? Isbn {get;set;} public int? PublishedYear {get;set;}
 public string? CoverUrl {get;set;} public string OpenLibraryKey {get;set;} = "";
 public string? Description {get;set;} public Genre Genre {get;set;}
 public decimal Price {get;set;} public int Stock {get;set;}
}
