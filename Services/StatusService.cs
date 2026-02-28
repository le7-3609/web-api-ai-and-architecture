using AutoMapper;
using DTO;
using Entities;
using Repositories;

namespace Services
{
    public class StatusService : IStatusService
    {
        private readonly IStatusRepository _statusRepository;
        private readonly IMapper _mapper;

        public StatusService(IStatusRepository statusRepository, IMapper mapper)
        {
            _statusRepository = statusRepository;
            _mapper = mapper;
        }

        public async Task<IEnumerable<StatusesDTO>> GetAllStatusesAsync()
        {
            var statuses = await _statusRepository.GetAllStatusesAsync();
            return _mapper.Map<IEnumerable<StatusesDTO>>(statuses);
        }

        public async Task<StatusesDTO?> GetStatusByIdAsync(int id)
        {
            Status? status = await _statusRepository.GetStatusByIdAsync(id);
            return status == null ? null : _mapper.Map<StatusesDTO>(status);
        }
    }
}
