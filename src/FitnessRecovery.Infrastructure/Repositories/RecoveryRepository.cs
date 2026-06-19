using System;
using System.Linq;
using System.Threading.Tasks;
using FitnessRecovery.Features.Recovery.Contracts;
using FitnessRecovery.Features.Recovery.Domain;
using FitnessRecovery.Infrastructure.Persistence;
using FitnessRecovery.SharedKernel.Models;
using Microsoft.EntityFrameworkCore;

namespace FitnessRecovery.Infrastructure.Repositories;

public class RecoveryRepository : IRecoveryRepository
{
    private readonly ApplicationDbContext _context;

    public RecoveryRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<RecoveryAnalysis?> GetByDateAsync(Guid userId, DateOnly date)
    {
        return await _context.Set<RecoveryAnalysis>()
            .FirstOrDefaultAsync(r => r.UserId == userId && r.AnalysisDate == date);
    }

    public async Task<PagedList<RecoveryAnalysis>> GetPagedByUserIdAsync(Guid userId, int page, int pageSize)
    {
        var query = _context.Set<RecoveryAnalysis>()
            .Where(r => r.UserId == userId)
            .OrderByDescending(r => r.AnalysisDate);

        var totalItems = await query.CountAsync();

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedList<RecoveryAnalysis>(items, page, pageSize, totalItems);
    }

    public async Task AddAsync(RecoveryAnalysis analysis)
    {
        await _context.Set<RecoveryAnalysis>().AddAsync(analysis);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(RecoveryAnalysis analysis)
    {
        var entry = _context.Entry(analysis);
        if (entry.State == EntityState.Detached)
        {
            _context.Set<RecoveryAnalysis>().Update(analysis);
        }
        await _context.SaveChangesAsync();
    }
}
