using System;
using System.Threading.Tasks;
using FitnessRecovery.Features.Recommendation.Domain;
using FitnessRecovery.SharedKernel.Models;

namespace FitnessRecovery.Features.Recommendation.Contracts;

public interface IRecommendationRepository
{
    Task<Domain.Recommendation?> GetByAnalysisIdAsync(Guid analysisId);
    Task<PagedList<Domain.Recommendation>> GetPagedByUserIdAsync(Guid userId, int page, int pageSize);
    Task AddAsync(Domain.Recommendation recommendation);
    Task UpdateAsync(Domain.Recommendation recommendation);
    Task DeleteAsync(Domain.Recommendation recommendation);
}
