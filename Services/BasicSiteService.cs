using AutoMapper;
using DTO;
using Entities;
using Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services
{
    public class BasicSiteService : IBasicSiteService
    {

        private readonly IBasicSiteRepository _basicSiteRepository;
        private readonly IMapper _mapper;
        public BasicSiteService(IMapper mapper, IBasicSiteRepository basicSiteRepository)
        {
            _mapper = mapper;
            _basicSiteRepository = basicSiteRepository;
        }

        async public Task<BasicSiteDTO?> GetBasicSiteByIdAsync(int id)
        {
            BasicSite? basicSite = await _basicSiteRepository.GetBasicSiteByIdAsync(id);
            return _mapper.Map<BasicSiteDTO>(basicSite);

        }

        public async Task<double> GetBasicSitePriceAsync(long basicSiteId)
        {
            return await _basicSiteRepository.GetBasicSitePriceAsync(basicSiteId);
        }

        async public Task UpdateBasicSiteAsync(int id, UpdateBasicSiteDTO dto)
        {
            BasicSite basicSite = _mapper.Map<BasicSite>(dto);
            await _basicSiteRepository.UpdateBasicSiteAsync(id, basicSite);

        }

        async public Task<BasicSiteDTO> AddBasicSiteAsync(AddBasicSiteDTO dto)
        {
            BasicSite basicSite = _mapper.Map<BasicSite>(dto);
            BasicSite newBasicSite = await _basicSiteRepository.AddBasicSiteAsync(basicSite);
            return _mapper.Map<BasicSiteDTO>(newBasicSite);
        }
    }
}
