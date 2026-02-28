using Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using DTO;
using Entities;
namespace Services
{
    public class ProductService : IProductService
    {

        private readonly IProductRepository _productRepository;
        private readonly IMapper _mapper;
        public ProductService(IProductRepository productRepository, IMapper mapper)
        {
            _productRepository = productRepository;
            _mapper = mapper;
        }
        async public Task<(IEnumerable<ProductDTO>, int TotalCount)> GetProductsAsync(int position, int skip, string? desc, int?[] subCategoryIds)
        {
            var (products, totalCount) = await _productRepository.GetProductsAsync(position, skip, desc, subCategoryIds);
            var productsRes = _mapper.Map<IEnumerable<ProductDTO>>(products);
            return (productsRes, TotalCount: totalCount);
        }

        async public Task<ProductDTO?> GetProductByIdAsync(int Id)
        {
            Product? product = await _productRepository.GetProductByIdAsync(Id);
            return _mapper.Map<ProductDTO>(product);
        }

        async public Task UpdateProductAsync(int id, AdminProductDTO dto)
        {
            if (string.IsNullOrWhiteSpace(dto.ProductName))
            {
                throw new ArgumentException("ProductName cannot be empty");
            }

            Product product = _mapper.Map<Product>(dto);
            product.ProductId = id; 
            product.ProductPrompt = string.IsNullOrWhiteSpace(dto.ProductPrompt) ? product.ProductPrompt ?? "" : dto.ProductPrompt;
            await _productRepository.UpdateProductAsync(id, product);
        }


        async public Task<ProductDTO> AddProductAsync(AdminProductDTO dto)
        {
            if (string.IsNullOrWhiteSpace(dto.ProductName))
            {
                throw new ArgumentException("ProductName cannot be empty");
            }

            Product product = _mapper.Map<Product>(dto);
            product.ProductPrompt = string.IsNullOrWhiteSpace(dto.ProductPrompt) ? "" : dto.ProductPrompt;
            product = await _productRepository.AddProductAsync(product);
            return _mapper.Map<ProductDTO>(product);
        }

        async public Task<bool> DeleteProductAsync(int id)
        {
            var product = await _productRepository.GetProductByIdAsync(id);
            if (product == null)
            {
                return false;
            }

            if (await _productRepository.HasOrderItemsByProductIdAsync(id))
            {
                throw new InvalidOperationException("Cannot delete product that has existing orders.");
            }

            await _productRepository.RemoveCartItemsByProductIdAsync(id);
            return await _productRepository.DeleteProductAsync(id);
        }
    }
}
