using System;
using System.Linq;
using System.Threading.Tasks;
using FitnessRecovery.Features.Health.Contracts;
using FitnessRecovery.Features.Health.Domain;
using FitnessRecovery.Infrastructure.Persistence;
using FitnessRecovery.SharedKernel.Models;
using Microsoft.EntityFrameworkCore;

namespace FitnessRecovery.Infrastructure.Repositories;

public class HealthRecordRepository : IHealthRecordRepository
{
    private readonly ApplicationDbContext _context;

    public HealthRecordRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<HealthRecord?> GetByDateAsync(Guid userId, DateOnly date)
    {
        return await _context.HealthRecords
            .FirstOrDefaultAsync(h => h.UserId == userId && h.RecordDate == date);
    }

    public async Task<PagedList<HealthRecord>> GetPagedByUserIdAsync(Guid userId, int page, int pageSize)
    {
        var query = _context.HealthRecords
            .Where(h => h.UserId == userId)
            .OrderByDescending(h => h.RecordDate);

        var totalItems = await query.CountAsync();

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedList<HealthRecord>(items, page, pageSize, totalItems);
    }

    public async Task AddAsync(HealthRecord record)
    {
        await _context.HealthRecords.AddAsync(record);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(HealthRecord record)
    {
        var entry = _context.Entry(record);
        if (entry.State == EntityState.Detached)
        {
            _context.HealthRecords.Update(record);
        }
        await _context.SaveChangesAsync();
    }
}
