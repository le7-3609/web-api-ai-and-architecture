using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DTO
{
    public

    record BasicSiteDTO
    {
        public long BasicSiteId { get; init; }
        public string SiteName { get; init; }
        public double Price { get; init; }
        public string UserDescreption { get; init; }
        public string SiteTypeName { get; init; }
        public string PlatformName { get; init; }
        public long? PlatformId { get; init; }
        public long SiteTypeId { get; init; }
        public string SiteTypeDescreption { get; init; }
    }

    public record UpdateBasicSiteDTO
    (
        long BasicSiteId,
        string SiteName,
        string UserDescreption,
        long SiteTypeId,
        long? PlatformId
    );
    public record AddBasicSiteDTO
    (
        string SiteName,
        string UserDescreption,
        long SiteTypeId,
        long? PlatformId
    );
}