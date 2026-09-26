using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace LamuFlix.Web.Models.Helper;

public interface IPagedListing
{
    int CurrentPage { get; }

    bool HasPreviousPage { get; }
    bool HasNextPage { get; }

    int PagingStart { get; }
    int PagingEnd { get; }
    int TotalPages { get; }
}

public class PagedListing<T> : IPagedListing
{
    public IList<T> Items { get; private set; }

    [DisplayFormat(DataFormatString = "{0:n0}")]
    public int TotalItems { get; private set; }

    public int ItemsPerPage { get; private set; }

    public int CurrentPage { get; private set; }

    public bool HasPreviousPage { get; private set; }
    public bool HasNextPage { get; private set; }

    public int PagingStart { get; private set; }
    public int PagingEnd { get; private set; }
    public int TotalPages { get; private set; }

    public PagedListing(IList<T> items, int totalItems, int page, int pageSize)
    {
        Items = items;

        TotalItems = totalItems;
        ItemsPerPage = pageSize;
        CurrentPage = page;

        // Total Pages
        TotalPages = (int)(Math.Ceiling((decimal)TotalItems / (decimal)ItemsPerPage));

        // Set PagerStart
        var pagingStart = CurrentPage - 5;
        var pagingEnd = CurrentPage + 4;

        if (pagingStart <= 0)
        {
            pagingEnd -= (pagingStart - 1);
            pagingStart = 1;
        }
        if (pagingEnd > TotalPages)
        {
            pagingEnd = TotalPages;
            if (pagingEnd > 10)
            {
                pagingStart = pagingEnd - 9;
            }
        }

        PagingStart = pagingStart;
        PagingEnd = pagingEnd;

        // Has?
        HasPreviousPage = CurrentPage > 1;
        HasNextPage = CurrentPage < TotalPages;
    }
}