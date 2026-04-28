using System.Text.Json;
using DTO;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace Services
{
    public class ProductCacheService : IProductCacheService
    {
        private const string VersionKey = "products:version";
        private static readonly TimeSpan SingleProductTtl = TimeSpan.FromMinutes(10);
        private static readonly TimeSpan ProductListTtl = TimeSpan.FromMinutes(5);

        private readonly IDatabase _db;
        private readonly ILogger<ProductCacheService> _logger;

        public ProductCacheService(IConnectionMultiplexer redis, ILogger<ProductCacheService> logger)
        {
            _db = redis.GetDatabase();
            _logger = logger;
        }

        public async Task<ProductDTO?> GetProductAsync(long id)
        {
            try
            {
                var value = await _db.StringGetAsync(ProductKey(id));
                if (value.IsNullOrEmpty)
                {
                    _logger.LogDebug("Cache miss for product {Id}", id);
                    return null;
                }

                _logger.LogDebug("Cache hit for product {Id}", id);
                return JsonSerializer.Deserialize<ProductDTO>(value!);
            }
            catch (RedisException ex)
            {
                _logger.LogWarning(ex, "Redis error on GetProductAsync for id {Id}", id);
                return null;
            }
        }

        public async Task SetProductAsync(long id, ProductDTO product)
        {
            try
            {
                var serialized = JsonSerializer.Serialize(product);
                await _db.StringSetAsync(ProductKey(id), serialized, SingleProductTtl, When.Always, CommandFlags.None);
            }
            catch (RedisException ex)
            {
                _logger.LogWarning(ex, "Redis error on SetProductAsync for id {Id}", id);
            }
        }

        public async Task<(IEnumerable<ProductDTO>? Items, int TotalCount)> GetProductListAsync(string cacheKey)
        {
            try
            {
                var value = await _db.StringGetAsync(cacheKey);
                if (value.IsNullOrEmpty)
                {
                    _logger.LogDebug("Cache miss for product list key {Key}", cacheKey);
                    return (null, 0);
                }

                _logger.LogDebug("Cache hit for product list key {Key}", cacheKey);
                var cached = JsonSerializer.Deserialize<CachedProductList>(value!);
                return cached is null ? (null, 0) : (cached.Items, cached.TotalCount);
            }
            catch (RedisException ex)
            {
                _logger.LogWarning(ex, "Redis error on GetProductListAsync for key {Key}", cacheKey);
                return (null, 0);
            }
        }

        public async Task SetProductListAsync(string cacheKey, IEnumerable<ProductDTO> items, int totalCount)
        {
            try
            {
                var payload = new CachedProductList(items.ToList(), totalCount);
                var serialized = JsonSerializer.Serialize(payload);
                await _db.StringSetAsync(cacheKey, serialized, ProductListTtl);
            }
            catch (RedisException ex)
            {
                _logger.LogWarning(ex, "Redis error on SetProductListAsync for key {Key}", cacheKey);
            }
        }

        public async Task InvalidateProductAsync(long id)
        {
            try
            {
                await _db.KeyDeleteAsync(ProductKey(id));
                _logger.LogDebug("Invalidated cache for product {Id}", id);
            }
            catch (RedisException ex)
            {
                _logger.LogWarning(ex, "Redis error on InvalidateProductAsync for id {Id}", id);
            }
        }

        public async Task InvalidateProductListsAsync()
        {
            try
            {
                await _db.StringIncrementAsync(VersionKey);
                _logger.LogDebug("Incremented product list cache version");
            }
            catch (RedisException ex)
            {
                _logger.LogWarning(ex, "Redis error on InvalidateProductListsAsync");
            }
        }

        public async Task<string> BuildListCacheKeyAsync(int position, int skip, string? desc, int?[] subCategoryIds)
        {
            long version = 0;
            try
            {
                var raw = await _db.StringGetAsync(VersionKey);
                if (!raw.IsNullOrEmpty)
                    version = (long)raw;
            }
            catch (RedisException ex)
            {
                _logger.LogWarning(ex, "Redis error reading version key; using version 0");
            }

            var ids = subCategoryIds?.Length > 0
                ? string.Join(",", subCategoryIds.Select(x => x?.ToString() ?? "null").OrderBy(x => x))
                : "none";

            return $"products:v{version}:{position}:{skip}:{desc ?? ""}:{ids}";
        }

        private static string ProductKey(long id) => $"product:{id}";

        private record CachedProductList(List<ProductDTO> Items, int TotalCount);
    }
}
