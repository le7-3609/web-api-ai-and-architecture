using AutoMapper;
using DTO;
using Entities;
using Microsoft.Identity.Client;
using Repositories;
using System.Text.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services
{
    public class GeminiService : IGeminiService
    {
        private readonly IGemini _gemini;
        private readonly IGeminiPromptsRepository _geminiPromptsRepository;
        private readonly ISubCategoryRepository _subCategoryRepository;
        private readonly IMainCategoryRepository _mainCategoryRepository;
        private readonly IMapper _mapper;

        public GeminiService(IGemini gemini, ISubCategoryRepository subCategoryRepository,
            IMainCategoryRepository mainCategoryRepository,
            IGeminiPromptsRepository geminiPromptsRepository, IMapper mapper)
        {
            _gemini = gemini;
            _subCategoryRepository = subCategoryRepository;
            _mainCategoryRepository = mainCategoryRepository;
            _geminiPromptsRepository = geminiPromptsRepository;
            _mapper = mapper;
        }

        public async Task<GeminiPromptDTO> AddGeminiForUserProductAsync(int? categoryId, string userRequest)
        {
            string requestForGemini = userRequest;
            string categoryName = "General";

            if (categoryId.HasValue)
            {
                SubCategory? subCategory = await _subCategoryRepository.GetSubCategoryByIdAsync(categoryId.Value);
                if (subCategory == null)
                {
                    throw new InvalidOperationException("The product id is incorrect");
                }

                categoryName = subCategory.SubCategoryName;
                requestForGemini = BuildRequestWithSubCategoryContext(userRequest, subCategory);
            }

            string? result = await _gemini.RunGeminiForUserProduct(requestForGemini, categoryName);
            string? promptValue = ExtractTechnicalValue(result);
            if (string.IsNullOrWhiteSpace(promptValue))
            {
                throw new InvalidOperationException("Failed to generate prompt");
            }

            GeminiPrompt prompt = new GeminiPrompt
            {
                Prompt = promptValue,
                SubCategoryId = categoryId
            };

            GeminiPrompt resultFromRepository = await _geminiPromptsRepository.AddPromptAsync(prompt);
            return _mapper.Map<GeminiPromptDTO>(resultFromRepository);
        }

        public async Task<GeminiPromptDTO> AddGeminiForUserCategoryAsync(int subCategoryId, string userRequest)
        {
            SubCategory? subCategory = await _subCategoryRepository.GetSubCategoryByIdAsync(subCategoryId);
            if (subCategory == null)
            {
                throw new InvalidOperationException("The sub category id is incorrect");
            }

            MainCategory? mainCategory = await _mainCategoryRepository.GetMainCategoryByIdAsync((int)subCategory.MainCategoryId);
            if (mainCategory == null)
            {
                throw new InvalidOperationException("The main category id is incorrect");
            }

            string requestForGemini = BuildRequestWithMainCategoryContext(userRequest, subCategory, mainCategory);
            string? result = await _gemini.RunGeminiForFillCategory(requestForGemini, mainCategory.MainCategoryName);
            string? promptValue = ExtractTechnicalValue(result);
            if (string.IsNullOrWhiteSpace(promptValue))
            {
                throw new InvalidOperationException("Failed to generate prompt");
            }

            GeminiPrompt prompt = new GeminiPrompt
            {
                Prompt = promptValue,
                SubCategoryId = subCategory.SubCategoryId
            };

            GeminiPrompt resultFromRepository = await _geminiPromptsRepository.AddPromptAsync(prompt);
            return _mapper.Map<GeminiPromptDTO>(resultFromRepository);
        }

        public async Task<GeminiPromptDTO> AddGeminiForBasicSiteAsync(string userRequest)
        {
            string? result = await _gemini.RunGeminiForFillBasicSite(userRequest);
            string? promptValue = ExtractTechnicalValue(result);
            if (string.IsNullOrWhiteSpace(promptValue))
            {
                throw new InvalidOperationException("Failed to generate prompt");
            }

            GeminiPrompt prompt = new GeminiPrompt
            {
                Prompt = promptValue,
                SubCategoryId = null
            };

            GeminiPrompt resultFromRepository = await _geminiPromptsRepository.AddPromptAsync(prompt);
            return _mapper.Map<GeminiPromptDTO>(resultFromRepository);

        }

        public async Task<GeminiPromptDTO> UpdateGeminiForUserProductAsync(long promptId, string userRequest)
        {
            GeminiPrompt? checkIfThePromptExist = await _geminiPromptsRepository.GetByIDPromptAsync(promptId);

            if (checkIfThePromptExist == null)
            {
                throw new InvalidOperationException("The prompt id is incorrect");
            }
            string requestForGemini = userRequest;
            string categoryName = "General";

            if (checkIfThePromptExist.SubCategoryId.HasValue)
            {
                SubCategory? subCategory = await _subCategoryRepository.GetSubCategoryByIdAsync((int)checkIfThePromptExist.SubCategoryId.Value);
                if (subCategory == null)
                {
                    throw new InvalidOperationException("The product id is incorrect");
                }

                categoryName = subCategory.SubCategoryName;
                requestForGemini = BuildRequestWithSubCategoryContext(userRequest, subCategory);
            }

            string? result = await _gemini.RunGeminiForUserProduct(requestForGemini, categoryName);
            string? promptValue = ExtractTechnicalValue(result);
            if (string.IsNullOrWhiteSpace(promptValue))
            {
                throw new InvalidOperationException("Failed to update prompt");
            }

            checkIfThePromptExist.Prompt = promptValue;
            await _geminiPromptsRepository.UpdatePromptAsync(promptId, checkIfThePromptExist);
            return _mapper.Map<GeminiPromptDTO>(checkIfThePromptExist);
        }

        public async Task<GeminiPromptDTO> UpdateGeminiForUserCategoryAsync(long promptId, string userRequest)
        {
            GeminiPrompt? existingPrompt = await _geminiPromptsRepository.GetByIDPromptAsync(promptId);
            if (existingPrompt == null)
            {
                throw new InvalidOperationException("The prompt id is incorrect");
            }

            if (!existingPrompt.SubCategoryId.HasValue)
            {
                throw new InvalidOperationException("This prompt is not linked to a sub category");
            }

            SubCategory? subCategory = await _subCategoryRepository.GetSubCategoryByIdAsync((int)existingPrompt.SubCategoryId.Value);
            if (subCategory == null)
            {
                throw new InvalidOperationException("The sub category id is incorrect");
            }

            MainCategory? mainCategory = await _mainCategoryRepository.GetMainCategoryByIdAsync((int)subCategory.MainCategoryId);
            if (mainCategory == null)
            {
                throw new InvalidOperationException("The main category id is incorrect");
            }

            string requestForGemini = BuildRequestWithMainCategoryContext(userRequest, subCategory, mainCategory);
            string? result = await _gemini.RunGeminiForFillCategory(requestForGemini, mainCategory.MainCategoryName);
            string? promptValue = ExtractTechnicalValue(result);
            if (string.IsNullOrWhiteSpace(promptValue))
            {
                throw new InvalidOperationException("Failed to update prompt");
            }

            existingPrompt.Prompt = promptValue;
            await _geminiPromptsRepository.UpdatePromptAsync(promptId, existingPrompt);
            return _mapper.Map<GeminiPromptDTO>(existingPrompt);
        }

        public async Task<GeminiPromptDTO> UpdateGeminiForBasicSiteAsync(long promptId, string userRequest)
        {
            GeminiPrompt? existingPrompt = await _geminiPromptsRepository.GetByIDPromptAsync(promptId);
            if (existingPrompt == null)
            {
                throw new InvalidOperationException("The prompt id is incorrect");
            }

            string? result = await _gemini.RunGeminiForFillBasicSite(userRequest);
            string? promptValue = ExtractTechnicalValue(result);
            if (string.IsNullOrWhiteSpace(promptValue))
            {
                throw new InvalidOperationException("Failed to update prompt");
            }

            existingPrompt.Prompt = promptValue;
            await _geminiPromptsRepository.UpdatePromptAsync(promptId, existingPrompt);
            return _mapper.Map<GeminiPromptDTO>(existingPrompt);
        }

        public async Task<GeminiPromptDTO?> GetByIdPromptAsync(long promptId)
        {
            GeminiPrompt? prompt = await _geminiPromptsRepository.GetByIDPromptAsync(promptId);
            return prompt != null ? _mapper.Map<GeminiPromptDTO>(prompt) : null;
        }

        private static string BuildRequestWithSubCategoryContext(string userRequest, SubCategory subCategory)
        {
            return $"User Request: {userRequest}\nSub Category Name: {subCategory.SubCategoryName}\nSub Category Prompt: {subCategory.SubCategoryPrompt}";
        }

        private static string BuildRequestWithMainCategoryContext(string userRequest, SubCategory subCategory, MainCategory mainCategory)
        {
            return $"User Request: {userRequest}\nMain Category Name: {mainCategory.MainCategoryName}\nMain Category Prompt: {mainCategory.MainCategoryPrompt}\nSub Category Name: {subCategory.SubCategoryName}\nSub Category Prompt: {subCategory.SubCategoryPrompt}";
        }

        private static string? ExtractTechnicalValue(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
            {
                return null;
            }

            string clean = raw.Trim();
            if (clean.StartsWith("```json", StringComparison.OrdinalIgnoreCase))
            {
                clean = clean.Replace("```json", "", StringComparison.OrdinalIgnoreCase)
                    .Replace("```", "", StringComparison.OrdinalIgnoreCase)
                    .Trim();
            }
            else if (clean.StartsWith("```", StringComparison.OrdinalIgnoreCase))
            {
                clean = clean.Replace("```", "", StringComparison.OrdinalIgnoreCase).Trim();
            }

            try
            {
                using JsonDocument doc = JsonDocument.Parse(clean);
                if (doc.RootElement.TryGetProperty("technical_value", out JsonElement technicalValue))
                {
                    return technicalValue.GetString();
                }
            }
            catch
            {
                return clean;
            }

            return clean;
        }
        public async Task DeletePromptAsync(long promptId)
        {
            GeminiPrompt? checkIfThePromptExist = await _geminiPromptsRepository.GetByIDPromptAsync(promptId);
            if (checkIfThePromptExist == null)
            {
                throw new InvalidOperationException("The prompt id is incorrect");
            }
            await _geminiPromptsRepository.DeletePromptAsync(promptId);
        }
    }
}