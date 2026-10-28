using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using System.Text.Json.Serialization;
using BookStore.Application;
namespace BookStore.Infrastructure;
public sealed class OpenLibraryCatalog(HttpClient http,ILogger<OpenLibraryCatalog> logger):ICatalog {
 public async Task<IReadOnlyList<CatalogBook>> Search(string query,CancellationToken ct) {
  try {
   var url="search.json?q="+Uri.EscapeDataString(query)+"&fields=key,title,author_name,first_publish_year,isbn,cover_i&limit=10";
   using var response=await http.GetAsync(url,ct);response.EnsureSuccessStatusCode();
   var data=await response.Content.ReadFromJsonAsync<SearchResponse>(cancellationToken:ct);
   return (data?.Docs??[]).Where(x=>x.Key?.StartsWith("/works/")==true && !string.IsNullOrWhiteSpace(x.Title)).Select(x=>new CatalogBook(
    x.Key!,x.Title!,string.Join(", ",x.Authors??[]),x.Isbns?.FirstOrDefault(),x.Year,
    x.CoverId is >0 ? $"https://covers.openlibrary.org/b/id/{x.CoverId}-M.jpg" : x.Isbns?.FirstOrDefault() is string isbn ? $"https://covers.openlibrary.org/b/isbn/{Uri.EscapeDataString(isbn)}-M.jpg" : null)).ToList();
  } catch(OperationCanceledException) when(!ct.IsCancellationRequested) { logger.LogWarning("Open Library timeout for query {Query}",query);throw new AppException(503,"Catalog timed out. Please retry."); }
  catch(HttpRequestException ex) {logger.LogWarning(ex,"Open Library unavailable for query {Query}",query);throw new AppException(503,"Catalog unavailable. Please retry.");}
 }
 private sealed record SearchResponse([property:JsonPropertyName("docs")] List<SearchDoc>? Docs);
 private sealed record SearchDoc([property:JsonPropertyName("key")]string? Key,[property:JsonPropertyName("title")]string? Title,[property:JsonPropertyName("author_name")]string[]? Authors,[property:JsonPropertyName("first_publish_year")]int? Year,[property:JsonPropertyName("isbn")]string[]? Isbns,[property:JsonPropertyName("cover_i")]int? CoverId);
}
