using System;
using System.Threading.Tasks;
using FitnessRecovery.Features.Recovery.Domain;
using FitnessRecovery.SharedKernel.Models;

namespace FitnessRecovery.Features.Recovery.Contracts;

public interface IRecoveryRepository
{
    Task<RecoveryAnalysis?> GetByDateAsync(Guid userId, DateOnly date);
    Task<PagedList<RecoveryAnalysis>> GetPagedByUserIdAsync(Guid userId, int page, int pageSize);
    Task AddAsync(RecoveryAnalysis analysis);
    Task UpdateAsync(RecoveryAnalysis analysis);
}
