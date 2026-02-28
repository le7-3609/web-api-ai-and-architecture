using AutoMapper;
using DTO;
using Entities;
using Repositories;
using System.Text.Json;

namespace Services
{
    public class SiteTypeService : ISiteTypeService
    {
        private readonly ISiteTypeRepository _siteTypeRepository;
        private readonly IMapper _mapper;

        public SiteTypeService(ISiteTypeRepository siteTypeRepository, IMapper mapper)
        {
            _siteTypeRepository = siteTypeRepository;
            _mapper = mapper;
        }

        public async Task<IEnumerable<SiteTypeDTO>?> GetAllAsync()
        {
            var siteTypes = await _siteTypeRepository.GetAllAsync();
            if (siteTypes == null)
                return null;
            return _mapper.Map<IEnumerable<SiteTypeDTO>>(siteTypes);

        }
        public async Task<SiteTypeDTO?> GetByIdAsync(int id)
        {
            var siteType = await _siteTypeRepository.GetByIdAsync(id);
            return _mapper.Map<SiteTypeDTO>(siteType);
        }
        public async Task<SiteTypeDTO?> UpdateByMngAsync(int id, SiteTypeDTO dto)
        {
            var siteType = await _siteTypeRepository.GetByIdAsync(id);
            if (siteType == null)
                return null;

            var existingWithSameName = await _siteTypeRepository.GetByNameAsync(dto.SiteTypeName);
            if (existingWithSameName != null && existingWithSameName.SiteTypeId != id)
            {
                throw new InvalidOperationException($"SiteType with name '{dto.SiteTypeName}' already exists");
            }

            _mapper.Map(dto, siteType);
            var updated = await _siteTypeRepository.UpdateByMngAsync(siteType);
            return _mapper.Map<SiteTypeDTO>(updated);
        }
    }
}
