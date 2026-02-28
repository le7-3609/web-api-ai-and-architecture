using AutoMapper;
using DTO;
using Entities;
using Microsoft.EntityFrameworkCore;
using Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services
{
    public class SubCategoryService : ISubCategoryService
    {
        private readonly ISubCategoryRepository _subCategoryRepository;
        private readonly IMapper _mapper;
        public SubCategoryService(ISubCategoryRepository subCategoryRepository, IMapper mapper)
        {
            _mapper = mapper;
            this._subCategoryRepository = subCategoryRepository;
        }

        async public Task<(IEnumerable<SubCategoryDTO>, int TotalCount)> GetSubCategoryAsync(int position, int skip, string? desc, int?[] mainCategoryIds)
        {
            var (subCategories, totalCount) = await _subCategoryRepository.GetSubCategoryAsync(position, skip, desc, mainCategoryIds);
            var subCategoriesRes = _mapper.Map<IEnumerable<SubCategoryDTO>>(subCategories);
            return (subCategoriesRes, TotalCount: totalCount);
        }

        async public Task<SubCategoryDTO?> GetSubCategoryByIdAsync(int id)
        {
            SubCategory? category = await _subCategoryRepository.GetSubCategoryByIdAsync(id);
            return _mapper.Map<SubCategoryDTO>(category);
        }

        async public Task UpdateSubCategoryAsync(int id, SubCategoryDTO dto)
        {
            var existingWithSameName = await _subCategoryRepository.GetByNameAsync(dto.SubCategoryName);
            if (existingWithSameName != null && existingWithSameName.SubCategoryId != id)
            {
                throw new InvalidOperationException($"SubCategory with name '{dto.SubCategoryName}' already exists");
            }
            
            SubCategory category = _mapper.Map<SubCategory>(dto);
            category.SubCategoryPrompt = "vfsghhfg";
            await _subCategoryRepository.UpdateSubCategoryAsync(id, category);

        }


        async public Task<SubCategoryDTO> AddSubCategoryAsync(AddSubCategoryDTO dto)
        {
            // Check if a SubCategory with the same name already exists
            var existingWithSameName = await _subCategoryRepository.GetByNameAsync(dto.SubCategoryName);
            if (existingWithSameName != null)
            {
                throw new InvalidOperationException($"SubCategory with name '{dto.SubCategoryName}' already exists");
            }
            
            // Validate MainCategoryId exists
            var mainCategoryExists = await _subCategoryRepository.MainCategoryExistsAsync((int)dto.MainCategoryId);
            if (!mainCategoryExists)
            {
                throw new InvalidOperationException($"MainCategory with ID {dto.MainCategoryId} does not exist");
            }
            
            SubCategory category = _mapper.Map<SubCategory>(dto);
            category.SubCategoryPrompt = "gfasdfghfh";
            category = await _subCategoryRepository.AddSubCategoryAsync(category);

            return _mapper.Map<SubCategoryDTO>(category);
        }
        async public Task<bool> DeleteSubCategoryAsync(int id)
        {
            var existing = await _subCategoryRepository.GetSubCategoryByIdAsync(id);
            if (existing == null)
            {
                return false;
            }

            if (await _subCategoryRepository.HasProductsAsync(id))
            {
                throw new InvalidOperationException("Cannot delete subcategory that has products.");
            }

            return await _subCategoryRepository.DeleteSubCategoryAsync(id);
        }
    }
}
