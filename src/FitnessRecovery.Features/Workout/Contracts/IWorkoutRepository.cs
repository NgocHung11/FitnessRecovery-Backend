using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FitnessRecovery.Features.Workout.Domain;
using FitnessRecovery.SharedKernel.Models;

namespace FitnessRecovery.Features.Workout.Contracts;

public interface IWorkoutRepository
{
    Task<WorkoutSession?> GetByIdAsync(Guid id);
    Task<PagedList<WorkoutSession>> GetPagedByUserIdAsync(Guid userId, int page, int pageSize);
    Task AddAsync(WorkoutSession session);
    Task UpdateAsync(WorkoutSession session);
    Task DeleteAsync(WorkoutSession session);
    Task<List<WorkoutSession>> GetWorkoutsForDateAsync(Guid userId, DateOnly date);
}
