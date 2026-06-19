using System;
using System.Linq;
using System.Threading.Tasks;
using FitnessRecovery.Features.Recommendation.Contracts;
using FitnessRecovery.Features.Recommendation.Domain;
using FitnessRecovery.Infrastructure.Persistence;
using FitnessRecovery.SharedKernel.Models;
using Microsoft.EntityFrameworkCore;

namespace FitnessRecovery.Infrastructure.Repositories;

public class RecommendationRepository : IRecommendationRepository
{
    private readonly ApplicationDbContext _context;

    public RecommendationRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Recommendation?> GetByAnalysisIdAsync(Guid analysisId)
    {
        return await _context.Set<Recommendation>()
            .FirstOrDefaultAsync(r => r.RecoveryAnalysisId == analysisId);
    }

    public async Task<PagedList<Recommendation>> GetPagedByUserIdAsync(Guid userId, int page, int pageSize)
    {
        var query = _context.Set<Recommendation>()
            .Where(r => r.UserId == userId)
            .OrderByDescending(r => r.CreatedAt);

        var totalItems = await query.CountAsync();

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedList<Recommendation>(items, page, pageSize, totalItems);
    }

    public async Task AddAsync(Recommendation recommendation)
    {
        await _context.Set<Recommendation>().AddAsync(recommendation);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Recommendation recommendation)
    {
        var entry = _context.Entry(recommendation);
        if (entry.State == EntityState.Detached)
        {
            _context.Set<Recommendation>().Update(recommendation);
        }
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Recommendation recommendation)
    {
        _context.Set<Recommendation>().Remove(recommendation);
        await _context.SaveChangesAsync();
    }
}
