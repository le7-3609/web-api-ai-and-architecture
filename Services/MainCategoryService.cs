using DTO;
using Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Entities;
using AutoMapper;
namespace Services
{
    public class MainCategoryService : IMainCategoryService
    {

        private readonly IMainCategoryRepository _mainCategoryRepository;
        private readonly IMapper _mapper;
        public MainCategoryService(IMainCategoryRepository mainCategoryReposetory, IMapper mapper)
        {
            _mainCategoryRepository = mainCategoryReposetory;
            _mapper = mapper;
        }

        async public Task<IEnumerable<MainCategoryDTO>> GetMainCategoryAsync()
        {
            var MainCategories = await _mainCategoryRepository.GetMainCategoriesAsync();
            var dto = _mapper.Map<IEnumerable<MainCategoryDTO>>(MainCategories);
            return dto;
        }

        async public Task<AddMainCategoryDTO> GetMainCategoryByIdAsync(int id)
        {
            var MainCategory = await _mainCategoryRepository.GetMainCategoryByIdAsync(id);
            var dto = _mapper.Map<AddMainCategoryDTO>(MainCategory);
            return dto;
        }


        async public Task<MainCategoryDTO> AddMainCategoryAsync(AddMainCategoryDTO dto)
        {
            if (string.IsNullOrWhiteSpace(dto.MainCategoryPrompt))
            {
                throw new ArgumentException("MainCategoryPrompt cannot be empty");
            }

            MainCategory mainCategory = _mapper.Map<MainCategory>(dto);
            if (string.IsNullOrWhiteSpace(mainCategory.MainCategoryPrompt))
            {
                mainCategory.MainCategoryPrompt = "Default prompt for " + mainCategory.MainCategoryName;
            }

            mainCategory = await _mainCategoryRepository.AddMainCategoryAsync(mainCategory);
            return _mapper.Map<MainCategoryDTO>(mainCategory);
        }

        async public Task<bool> UpdateMainCategoryAsync(int id, AddMainCategoryDTO dto)
        {
            var existingCategory = await _mainCategoryRepository.GetMainCategoryByIdAsync(id);
            if (existingCategory == null)
            {
                return false; // not found
            }

            MainCategory mainCategory = _mapper.Map<MainCategory>(dto);
            mainCategory.MainCategoryId = id;

            // Preserve existing prompt if not provided
            if (string.IsNullOrWhiteSpace(dto.MainCategoryPrompt))
            {
                mainCategory.MainCategoryPrompt = existingCategory.MainCategoryPrompt;
            }

            if (string.IsNullOrWhiteSpace(mainCategory.MainCategoryPrompt))
            {
                mainCategory.MainCategoryPrompt = "Default prompt for " + mainCategory.MainCategoryName;
            }

            await _mainCategoryRepository.UpdateMainCategoryAsync(mainCategory);
            return true;
        }

        async public Task<bool> DeleteMainCategoryAsync(int id)
        {
            var existing = await _mainCategoryRepository.GetMainCategoryByIdAsync(id);
            if (existing == null)
            {
                return false;
            }

            if (await _mainCategoryRepository.HasSubCategoriesAsync(id))
            {
                throw new InvalidOperationException("Cannot delete main category that has subcategories.");
            }

            return await _mainCategoryRepository.DeleteMainCategoryAsync(id);
        }
    }
}
