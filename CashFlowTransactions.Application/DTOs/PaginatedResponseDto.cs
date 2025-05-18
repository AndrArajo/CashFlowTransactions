using System.Collections.Generic;

namespace CashFlowTransactions.Application.DTOs
{
    public class PaginatedResponseDto<T>
    {
        public IEnumerable<T> Items { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        public int TotalPages { get; set; }
        public bool HasPreviousPage => PageNumber > 1;
        public bool HasNextPage => PageNumber < TotalPages;

        public PaginatedResponseDto(IEnumerable<T> items, int pageNumber, int pageSize, int totalCount, int totalPages)
        {
            Items = items;
            PageNumber = pageNumber;
            PageSize = pageSize;
            TotalCount = totalCount;
            TotalPages = totalPages;
        }

        public static PaginatedResponseDto<T> Create(IEnumerable<T> items, int pageNumber, int pageSize, int totalCount)
        {
            var totalPages = (int)System.Math.Ceiling(totalCount / (double)pageSize);
            return new PaginatedResponseDto<T>(items, pageNumber, pageSize, totalCount, totalPages);
        }
    }
} 