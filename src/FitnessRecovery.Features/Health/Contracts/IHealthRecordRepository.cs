using System;
using System.Threading.Tasks;
using FitnessRecovery.Features.Health.Domain;
using FitnessRecovery.SharedKernel.Models;

namespace FitnessRecovery.Features.Health.Contracts;

public interface IHealthRecordRepository
{
    Task<HealthRecord?> GetByDateAsync(Guid userId, DateOnly date);
    Task<PagedList<HealthRecord>> GetPagedByUserIdAsync(Guid userId, int page, int pageSize);
    Task AddAsync(HealthRecord record);
    Task UpdateAsync(HealthRecord record);
}
