using System.Text.Json;
using DTO;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace Services
{
    public class SubCategoryCacheService : ISubCategoryCacheService
    {
        private const string VersionKey = "subcategories:version";
        private static readonly TimeSpan SubCategoryListTtl = TimeSpan.FromMinutes(5);

        private readonly IDatabase _db;
        private readonly ILogger<SubCategoryCacheService> _logger;

        public SubCategoryCacheService(IConnectionMultiplexer redis, ILogger<SubCategoryCacheService> logger)
        {
            _db = redis.GetDatabase();
            _logger = logger;
        }

        public async Task<(IEnumerable<SubCategoryDTO>? Items, int TotalCount)> GetSubCategoryListAsync(string cacheKey)
        {
            try
            {
                var value = await _db.StringGetAsync(cacheKey);
                if (value.IsNullOrEmpty)
                {
                    _logger.LogInformation("[Cache] MISS list:{Key}", cacheKey);
                    return (null, 0);
                }

                _logger.LogInformation("[Cache] HIT  list:{Key}", cacheKey);
                var cached = JsonSerializer.Deserialize<CachedSubCategoryList>(value!);
                return cached is null ? (null, 0) : (cached.Items, cached.TotalCount);
            }
            catch (RedisException ex)
            {
                _logger.LogWarning(ex, "Redis error on GetSubCategoryListAsync for key {Key}", cacheKey);
                return (null, 0);
            }
        }

        public async Task SetSubCategoryListAsync(string cacheKey, IEnumerable<SubCategoryDTO> items, int totalCount)
        {
            try
            {
                var payload = new CachedSubCategoryList(items.ToList(), totalCount);
                var serialized = JsonSerializer.Serialize(payload);
                await _db.StringSetAsync(cacheKey, serialized, SubCategoryListTtl);
                _logger.LogInformation("[Cache] SET  list:{Key} ({Count} items, TTL {Ttl})", cacheKey, totalCount, SubCategoryListTtl);
            }
            catch (RedisException ex)
            {
                _logger.LogWarning(ex, "Redis error on SetSubCategoryListAsync for key {Key}", cacheKey);
            }
        }

        public async Task<string> BuildListCacheKeyAsync(int position, int skip, string? desc, int?[] mainCategoryIds)
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
                _logger.LogWarning(ex, "Redis error reading subcategory version key; using version 0");
            }

            var ids = mainCategoryIds?.Length > 0
                ? string.Join(",", mainCategoryIds.Select(x => x?.ToString() ?? "null").OrderBy(x => x))
                : "none";

            return $"subcategories:v{version}:{position}:{skip}:{desc ?? ""}:{ids}";
        }

        public async Task InvalidateSubCategoryListsAsync()
        {
            try
            {
                await _db.StringIncrementAsync(VersionKey);
                _logger.LogInformation("[Cache] INVALIDATED all subcategory lists (version bumped)");
            }
            catch (RedisException ex)
            {
                _logger.LogWarning(ex, "Redis error on InvalidateSubCategoryListsAsync");
            }
        }

        private record CachedSubCategoryList(List<SubCategoryDTO> Items, int TotalCount);
    }
}