using IPMS.Domain.Common;
using System;
using System.Collections.Generic;
using System.Linq;

namespace IPMS.Domain.ProjectManagement
{
    /// <summary>
    /// One row on the "Candidate Count" tab. Can be entered at three different
    /// levels of detail — the user picks whichever is convenient, all three are
    /// equally valid (see the "Draft validity philosophy" — presence is
    /// optional, correctness of whatever's present is not):
    ///   - PHASE level:          Stage is set, PostAssignmentId and
    ///                            SpecializationId are both null (a single
    ///                            total for the whole phase, e.g. "Preliminary: 502").
    ///   - POST level:            Stage + PostAssignmentId set, SpecializationId null.
    ///   - SPECIALIZATION level:  Stage + PostAssignmentId + SpecializationId all set.
    /// </summary>
    public class CandidateCount : BatchScopedEntity<long>
    {
        public string Stage { get; private set; }        // e.g. "Preliminary", "Main", "Interview", "Pre"
        public long? PostAssignmentId { get; private set; }
        public long? SpecializationId { get; private set; }
        public int Count { get; private set; }

        private CandidateCount() { }

        public static CandidateCount Create(string stage, long? postAssignmentId, long? specializationId, int count)
        {
            // A Specialization-level count without a Post makes no sense —
            // Specialization can't exist without its parent Post (same rule as
            // everywhere else), so this combination is rejected right at
            // creation rather than waiting for Send-to-Review to catch it.
            if (specializationId.HasValue && !postAssignmentId.HasValue)
                throw new System.InvalidOperationException("A Specialization-level count must also specify its parent Post.");

            return new CandidateCount { Stage = stage, PostAssignmentId = postAssignmentId, SpecializationId = specializationId, Count = count };
        }

        public IEnumerable<ValidationMessage> ValidateReferencesExistIn(
            IReadOnlyCollection<long> knownPostAssignmentIds, IReadOnlyCollection<long> knownSpecializationIds)
        {
            if (PostAssignmentId.HasValue && !knownPostAssignmentIds.Contains(PostAssignmentId.Value))
                yield return new ValidationMessage(Severity.Error,
                    $"Candidate Count ({Stage}) refers to a Post that no longer exists (Id {PostAssignmentId}).");

            if (SpecializationId.HasValue && !knownSpecializationIds.Contains(SpecializationId.Value))
                yield return new ValidationMessage(Severity.Error,
                    $"Candidate Count ({Stage}) refers to a Specialization that no longer exists (Id {SpecializationId}).");
        }
    }

}
