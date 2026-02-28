using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DTO
{
    public record SiteTypeDTO(
        long SiteTypeId,
        string SiteTypeName,
        double Price,
        string SiteTypeDescription
    );

    public record MngSiteTypeDTO(
         long SiteTypeId,
       string SiteTypeName,
         double Price,
       string SiteTypeDescription,
       string SiteTypeNamePrompt,
       string SiteTypeDescriptionPrompt
   ):SiteTypeDTO(SiteTypeId,SiteTypeName,Price,SiteTypeDescription);
    
    public record AddSiteTypeDTO
    (
        string SiteTypeName,
        double Price,
        string SiteTypeDescription,
        string SiteTypeNamePrompt,
        string SiteTypeDescriptionPrompt
    );
}