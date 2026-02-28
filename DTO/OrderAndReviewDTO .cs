using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
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
        public IEnumerable<OrderDetailsDTO> Orders { get; init; } = Enumerable.Empty<OrderDetailsDTO>();
        public double Total { get; init; }
    }

    public record AddReviewDTO
    (
        long OrderId,
        [Range(1, 5, ErrorMessage = "Review score must be between 1 and 5.")]
        short Score,
        string? Note,
        IFormFile? Image,
        string? ReviewImageUrl = null
    );

    public record ReviewDTO
    (
        long ReviewId,
        long OrderId,
        short Score,
        string Note,
        string ReviewImageUrl
    );

    public record StatusesDTO
    (
        long StatusId,
        string StatusName
    );

    public record ReviewSummaryDTO
    {
        public long ReviewId { get; init; }
        public string? ReviewImageUrl { get; init; }
        public string? Note { get; init; }
        public short Score { get; init; }
        public string SiteName { get; init; }
        public string SiteTypeName { get; init; }
    }
}