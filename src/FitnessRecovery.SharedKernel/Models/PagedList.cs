namespace FitnessRecovery.SharedKernel.Models;

public class PagedList<T>
{
    public PagedList(IEnumerable<T> items, int page, int pageSize, int totalItems)
    {
        Items = items.ToList();
        Page = page;
        PageSize = pageSize;
        TotalItems = totalItems;
        TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
    }

    public List<T> Items { get; }
    public int Page { get; }
    public int PageSize { get; }
    public int TotalItems { get; }
    public int TotalPages { get; }
    
    public bool HasNextPage => Page * PageSize < TotalItems;
    public bool HasPreviousPage => Page > 1;
}
