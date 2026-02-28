using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DTO
{
   public record MainCategoryDTO(
       long MainCategoryId,
        string MainCategoryName
    );

    public record AddMainCategoryDTO(
        [Required]
        string MainCategoryName,
        [Required]
        string MainCategoryPrompt
    );

    public record SubCategoryDTO
    (
        long SubCategoryId,
        long MainCategoryId,
        string SubCategoryName,
        string SubCategoryPrompt,
        string? ImageUrl,
        string CategoryDescription
    );

    public record AddSubCategoryDTO
    (
        [Required]
        long MainCategoryId,
        [Required]
        string SubCategoryName,
        [Required]
        string SubCategoryPrompt,
        string ImageUrl,
        [Required]
        string CategoryDescription
    );

     public record ProductDTO
     (     
         long ProductId,
         long SubCategoryId,
         string ProductName,
         string SubCategoryName,
         double Price,
         string ProductPrompt
     );
  

    public record UpdateProductDTO
    (
        [Required]
        long ProductId,
        [Required]
        long SubCategoryId,
        [Required]
        [StringLength(200)]
        string ProductName,
        [Required]
        double Price,
        [Required]
        string ProductPrompt
    );
    public record AddProductDTO
    (
        [Required]
        long SubCategoryId,
        [Required]
        [StringLength(200)]
        string ProductName,
        [Required]
        double Price,
        [Required]
        string ProductPrompt
    );

    public record AdminProductDTO
    (
        long? ProductId,
        [Required]
        long SubCategoryId,
        [Required]
        [StringLength(200)]
        string ProductName,
        [Required]
        double Price,
        string? ProductPrompt
    );
    public record AddPlatformDTO
    (
        string PlatformName,
        string PlatformPrompt
    );

    public record PaginatedResponse<T>(
        IEnumerable<T> Items,
        int TotalCount
    );
}