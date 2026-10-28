namespace BookStore.Application.Features.Catalog;

public record SearchCatalog(string Query) : IRequest<IReadOnlyList<CatalogBook>>;

public sealed class SearchCatalogHandler(ICatalog catalog) : IHandler<SearchCatalog, IReadOnlyList<CatalogBook>>
{
    public Task<IReadOnlyList<CatalogBook>> Handle(SearchCatalog q, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(q.Query) || q.Query.Trim().Length < 2)
            throw new AppException(400, "Search needs at least two characters");

        return catalog.Search(q.Query.Trim(), ct);
    }
}
