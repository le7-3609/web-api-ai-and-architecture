using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DTO
{
    public record CartDTO
    {
        public long CartId { get; init; }
        public long UserId { get; init; }
        public List<CartItemDTO> CartItems { get; init; } = new();
        public double BasicSitePrice { get; init; }
        public double TotalPrice { get; init; }
        public long? BasicSiteId { get; init; }
        public string? BasicSiteName { get; init; }
        public string? BasicSiteUserDescription { get; init; }
    }

    public record CartItemDTO
    {
        public long CartItemId { get; init; }
        public long CartId { get; init; }
        public string? SubCategoryName { get; init; }
        public string? ProductName { get; init; }
        public string? ImageUrl { get; init; }
        public string? CategoryDescription { get; init; }
        public double Price { get; init; }
        public string? PlatformName { get; init; }
        public long? PromptId { get; init; }
        public string? Prompt { get; init; }
        public string? UserDescription { get; init; }
        public bool IsActive { get; init; }
        public long ProductId { get; init; }
        public long? PlatformId { get; init; }
    }

    public record AddCartItemForEmptyProductDTO(
        long CartId,
        long ProductId,
        long? PlatformId,
        long? PromptId,
        string UserDescription
    );

    public record AddCartDTO(
        long UserId,
        long BasicSiteId
    );

    public record AddCartItemDTO(
        long CartId,
        long ProductId,
        long? PlatformId
    );

    public record UpdateCartItemDTO(
        int CartItemId,
        int? PlatformId,
        bool IsActive
    );

    public record ImportGuestCartItemDTO(
        int ProductId,
        int? PlatformId,
        long? PromptId,
        string UserDescription
    );

    public record ImportGuestCartDTO(List<ImportGuestCartItemDTO> Items);

    public record UpdateCartDTO(int? BasicSiteId);

    public record GuestCartImportResultDTO(
        long CartId,
        int AddedCount,
        int SkippedCount,
        List<int> SkippedProductIds
    );
}
