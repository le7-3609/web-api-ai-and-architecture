using Entities;
using Microsoft.Extensions.Hosting;
using Repositories;
using System.Text;

namespace Services
{
    public class OrderPromptBuilder : IOrderPromptBuilder
    {
        private readonly IBasicSiteRepository _basicSiteRepository;
        private readonly IProductRepository _productRepository;
        private readonly IGeminiPromptsRepository _geminiPromptsRepository;
        private readonly IHostEnvironment _hostEnvironment;

        public OrderPromptBuilder(
            IBasicSiteRepository basicSiteRepository,
            IProductRepository productRepository,
            IGeminiPromptsRepository geminiPromptsRepository,
            IHostEnvironment hostEnvironment)
        {
            _basicSiteRepository = basicSiteRepository;
            _productRepository = productRepository;
            _geminiPromptsRepository = geminiPromptsRepository;
            _hostEnvironment = hostEnvironment;
        }

        public async Task<string> BuildPromptAsync(long basicSiteId, IEnumerable<OrderItem> orderItems)
        {
            var basicSite = await _basicSiteRepository.GetBasicSiteByIdAsync((int)basicSiteId)
                ?? throw new KeyNotFoundException($"BasicSite {basicSiteId} not found.");

            var template = await LoadTemplateAsync();
            var corePurpose = await BuildCorePurposeAsync(basicSite);
            var platformPrompt = basicSite.Platform?.PlatformPrompt ?? string.Empty;
            var productsSection = await BuildProductsSectionAsync(orderItems);

            return FillTemplate(template, basicSite.SiteName, corePurpose, platformPrompt, productsSection);
        }

        private async Task<string> LoadTemplateAsync()
        {
            var path = Path.Combine(_hostEnvironment.ContentRootPath, "Prompts", "BasicPrompt.md");
            return await File.ReadAllTextAsync(path);
        }

        private async Task<string> BuildCorePurposeAsync(BasicSite basicSite)
        {
            if (!string.IsNullOrEmpty(basicSite.UserDescreption)
                && long.TryParse(basicSite.UserDescreption, out var promptId))
            {
                var geminiPrompt = await _geminiPromptsRepository.GetByIDPromptAsync(promptId);
                if (geminiPrompt != null)
                    return geminiPrompt.Prompt;
            }

            var siteType = basicSite.SiteType;
            if (siteType != null)
                return $"{siteType.SiteTypeNamePrompt} {siteType.SiteTypeDescriptionPrompt}";

            return string.Empty;
        }

        private async Task<string> BuildProductsSectionAsync(IEnumerable<OrderItem> orderItems)
        {
            var products = await _productRepository.GetProductsByIdsWithCategoriesAsync(
                orderItems.Select(oi => oi.ProductId).ToList());

            var productLookup = products.ToDictionary(p => p.ProductId);
            var sb = new StringBuilder();

            var grouped = orderItems
                .Where(oi => productLookup.ContainsKey(oi.ProductId))
                .GroupBy(oi => productLookup[oi.ProductId].SubCategory?.MainCategory)
                .Where(g => g.Key != null)
                .OrderBy(g => g.Key!.MainCategoryId);

            foreach (var mainGroup in grouped)
            {
                sb.Append("# ");
                sb.AppendLine(mainGroup.Key!.MainCategoryPrompt);
                sb.AppendLine();

                var subGroups = mainGroup
                    .GroupBy(oi => productLookup[oi.ProductId].SubCategory)
                    .Where(g => g.Key != null)
                    .OrderBy(g => g.Key!.SubCategoryId);

                foreach (var subGroup in subGroups)
                {
                    sb.Append("## ");
                    sb.AppendLine(subGroup.Key!.SubCategoryPrompt);
                    sb.AppendLine();

                    foreach (var orderItem in subGroup)
                    {
                        sb.Append("- ");
                        sb.AppendLine(productLookup[orderItem.ProductId].ProductPrompt);
                    }
                    sb.AppendLine();
                }
            }

            return sb.ToString();
        }

        private static string FillTemplate(
            string template, string siteName, string corePurpose,
            string platformPrompt, string productsSection)
        {
            return template
                .Replace("{{SiteName}}", siteName)
                .Replace("{{CorePurpose}}", corePurpose)
                .Replace("{{PlatformPrompt}}", platformPrompt)
                .Replace("{{ProductsSection}}", productsSection);
        }
    }
}
