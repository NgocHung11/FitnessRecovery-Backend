using FitnessRecovery.Features.Workout.Contracts;
using FitnessRecovery.Features.Workout.Domain;
using FitnessRecovery.Infrastructure.Persistence;
using FitnessRecovery.SharedKernel.Models;
using Microsoft.EntityFrameworkCore;

namespace FitnessRecovery.Infrastructure.Repositories;

public class WorkoutRepository : IWorkoutRepository
{
    private readonly ApplicationDbContext _context;

    public WorkoutRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<WorkoutSession?> GetByIdAsync(Guid id)
    {
        return await _context.WorkoutSessions.FindAsync(id);
    }

    public async Task<PagedList<WorkoutSession>> GetPagedByUserIdAsync(Guid userId, int page, int pageSize)
    {
        var query = _context.WorkoutSessions
            .Where(w => w.UserId == userId)
            .OrderByDescending(w => w.WorkoutDate);

        var totalItems = await query.CountAsync();
        
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedList<WorkoutSession>(items, page, pageSize, totalItems);
    }

    public async Task AddAsync(WorkoutSession session)
    {
        await _context.WorkoutSessions.AddAsync(session);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(WorkoutSession session)
    {
        var entry = _context.Entry(session);
        if (entry.State == EntityState.Detached)
        {
            _context.WorkoutSessions.Update(session);
        }
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(WorkoutSession session)
    {
        _context.WorkoutSessions.Remove(session);
        await _context.SaveChangesAsync();
    }

    public async Task<List<WorkoutSession>> GetWorkoutsForDateAsync(Guid userId, DateOnly date)
    {
        var targetDate = date.ToDateTime(TimeOnly.MinValue);
        return await _context.WorkoutSessions
            .Where(w => w.UserId == userId && w.WorkoutDate.Date == targetDate.Date)
            .ToListAsync();
    }
}
