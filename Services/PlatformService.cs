using AutoMapper;
using Entities;
using Microsoft.EntityFrameworkCore;
using Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DTO;
namespace  Services
{
    public class PlatformService : IPlatformService
    {
        private readonly IPlatformRepository _platformRepository;
        private readonly IMapper _mapper;
        public PlatformService(IPlatformRepository platformsReposetory, IMapper mapper)
        {

            _platformRepository = platformsReposetory;
            _mapper = mapper;
        }

        async public Task<IEnumerable<PlatformsDTO>> GetPlatformsAsync()
        {
            IEnumerable<Platform> platformList = await _platformRepository.GetPlatformsAsync();
            return _mapper.Map<IEnumerable<PlatformsDTO>>(platformList);
        }
        
        async public Task<PlatformsDTO?> GetPlatformByIdAsync(int id)
        {
            var platform = await _platformRepository.GetPlatformByIdAsync(id);
            return platform == null ? null : _mapper.Map<PlatformsDTO>(platform);
        }

        async public Task<PlatformsDTO?> AddPlatformAsync(string platformName)
        {
            var existing = await _platformRepository.GetPlatformByNameAsync(platformName);

            if (existing != null)
            {
                return null;
            }
            Platform platform = new Platform{PlatformName = platformName};
            platform = await _platformRepository.AddPlatformAsync(platform);
            //add prompt with gemini
            // dto.PlatformsPrompt = "fdfbnfgn";
            return _mapper.Map<PlatformsDTO>(platform);
        }

        async public Task<bool> UpdatePlatformAsync(int id, PlatformsDTO dto)
        {
            // Ensure platform exists by id
            var existingById = await _platformRepository.GetPlatformByIdAsync(id);
            if (existingById == null)
            {
                return false; // not found
            }

            // Check for name conflict with other records
            var existingByName = await _platformRepository.GetPlatformByNameAsync(dto.PlatformName);
            if (existingByName != null && existingByName.PlatformId != id)
            {
                return false; // conflict
            }

            Platform platform = _mapper.Map<Platform>(dto);
            // ensure id is set
            platform.PlatformId = id;

            return await _platformRepository.UpdatePlatformAsync(id, platform);
        }

        async public Task<bool> DeletePlatformAsync(int id)
        {
            if (id == 1)
            {
                throw new InvalidOperationException("Cannot delete the default platform.");
            }

            var platform = await _platformRepository.GetPlatformByIdAsync(id);
            if (platform == null)
            {
                return false;
            }

            await _platformRepository.ReassignPlatformReferencesAsync(id, 1);
            return await _platformRepository.DeletePlatformAsync(id);
        }
    }
}
