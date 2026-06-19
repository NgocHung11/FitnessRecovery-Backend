using System;

namespace FitnessRecovery.Features.Health.Queries.GetHealthRecordHistory;

public record GetHealthRecordHistoryQuery(Guid UserId, int Page, int PageSize);
