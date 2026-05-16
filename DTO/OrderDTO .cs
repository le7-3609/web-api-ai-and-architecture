using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DTO
{
    public record OrderItemDTO
    {
        public long OrderItemId { get; init; }
        public string ProductName { get; init; }
        public double Price { get; init; }
        public string PlatformName { get; init; }
        public long? PromptId { get; init; }
        public string? Prompt { get; init; }
        public string UserDescription { get; init; }
    }

    public record OrderSummaryDTO
    {
        public long OrderId { get; init; }
        public DateOnly? OrderDate { get; init; }
        public double OrderSum { get; init; }
        public string StatusName { get; init; }
        public string ReviewImageUrl { get; init; }
        public string SiteName { get; init; }
        public string SiteTypeName { get; init; }

    }

    public record OrderDetailsDTO : OrderSummaryDTO
    {
        public int OrderItemsCount { get; init; }
        public long UserId { get; init; }
        public long ReviewId { get; init; }
        public short Score { get; init; }
        public string SiteDescription { get; init; }
        public List<OrderItemDTO> Items { get; init; }
    }

    public record OrdersResponseDTO
    {
        public IEnumerable<OrderDetailsDTO> Orders { get; init; }
        public double Total { get; init; }
    }

    public record StatusesDTO
    (
        long StatusId,
        string StatusName
    );
}