using System;
using System.Threading.Tasks;
using FitnessRecovery.Features.Recovery.DTOs;

namespace FitnessRecovery.Features.Recovery.Contracts;

public interface IRecoveryCacheService
{
    Task<RecoveryAnalysisDto?> GetTodayRecoveryAsync(Guid userId);
    Task SetTodayRecoveryAsync(Guid userId, RecoveryAnalysisDto analysis, TimeSpan expiration);
    Task InvalidateTodayRecoveryAsync(Guid userId);
}
