using System.Collections.Generic;
using System.Linq;
using IPMS.Domain.Common;

namespace IPMS.Domain.Project
{
    /// <summary>
    /// One row on the "Exam Phases & Test Type" tab — e.g. "Post A + Post D ->
    /// Single Stage -> Objective and Descriptive".
    ///
    /// IMPORTANT: this does NOT reference a ProjectNumber anywhere. It only
    /// holds the Ids of whichever PostAssignments (and, optionally, specific
    /// Specializations under them) belong to this combination — and those Ids
    /// can come from ANY ProjectNumber in the Batch, mixed freely.
    ///
    /// Example: if ProjectNumber 1 declared PostAssignment A and B, and
    /// ProjectNumber 2 declared PostAssignment C and D, then "A + D" and
    /// "B + C" are both perfectly valid ExamPhaseDetail combinations, even
    /// though each one spans two different ProjectNumbers. ProjectNumber is
    /// only where a Post gets ITS Id — it plays no further role after that.
    /// </summary>
    public class ExamPhaseDetail : BatchScopedEntity<long>
    {
        // A PostAssignment Id by itself means "this whole Post" (every
        // Specialization under it, or the Post generally if it has none).
        // If the user picked ONE specific Specialization rather than the
        // whole Post, that goes in SpecializationIds instead — a combination
        // can freely mix "whole posts" and "specific specializations".
        public List<long> PostAssignmentIds { get; private set; } = new();
        public List<long> SpecializationIds { get; private set; } = new();

        public string ExamStage { get; private set; }        // e.g. "Single stage", "Preliminary", "Main"...
        public List<string> TestTypes { get; private set; } = new();   // e.g. "Objective", "Descriptive"

        private ExamPhaseDetail() { }

        public static ExamPhaseDetail Create(
            IEnumerable<long> postAssignmentIds,
            IEnumerable<long> specializationIds,
            string examStage,
            IEnumerable<string> testTypes)
        {
            return new ExamPhaseDetail
            {
                PostAssignmentIds = new List<long>(postAssignmentIds),
                SpecializationIds = new List<long>(specializationIds),
                ExamStage = examStage,
                TestTypes = new List<string>(testTypes)
            };
        }

        /// <summary>
        /// Part of the Option 3 referential-integrity rule (see the reference
        /// doc §8): never blocks anything by itself — Project.ValidateCurrentState()
        /// calls this and turns any orphan found into a blocking Error, but only
        /// at Send-to-Review / Approve time, never mid-edit.
        /// </summary>
        public IEnumerable<ValidationMessage> ValidateReferencesExistIn(
            IReadOnlyCollection<long> knownPostAssignmentIds,
            IReadOnlyCollection<long> knownSpecializationIds)
        {
            foreach (var id in PostAssignmentIds)
                if (!knownPostAssignmentIds.Contains(id))
                    yield return new ValidationMessage(Severity.Error,
                        $"Exam Phase '{ExamStage}' refers to a Post that no longer exists (Id {id}).");

            foreach (var id in SpecializationIds)
                if (!knownSpecializationIds.Contains(id))
                    yield return new ValidationMessage(Severity.Error,
                        $"Exam Phase '{ExamStage}' refers to a Specialization that no longer exists (Id {id}).");
        }
    }
}