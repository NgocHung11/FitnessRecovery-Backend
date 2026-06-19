using System;

namespace FitnessRecovery.Features.Recovery.Queries.GetRecoveryHistory;

public record GetRecoveryHistoryQuery(Guid UserId, int Page, int PageSize);
