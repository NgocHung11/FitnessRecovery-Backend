using System;

namespace FitnessRecovery.Features.Health.Queries.GetHealthRecord;

public record GetHealthRecordQuery(Guid UserId, DateOnly RecordDate);
