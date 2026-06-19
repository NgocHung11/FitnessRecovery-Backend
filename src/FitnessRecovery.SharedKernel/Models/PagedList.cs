using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace FitnessRecovery.SharedKernel.Models;

public class PagedList<T>
{
    [JsonConstructor]
    public PagedList()
    {
        Items = new List<T>();
    }

    public PagedList(IEnumerable<T> items, int page, int pageSize, int totalItems)
    {
        Items = items.ToList();
        Page = page;
        PageSize = pageSize;
        TotalItems = totalItems;
        TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
    }

    public List<T> Items { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalItems { get; init; }
    public int TotalPages { get; init; }
    
    public bool HasNextPage => Page * PageSize < TotalItems;
    public bool HasPreviousPage => Page > 1;
}
