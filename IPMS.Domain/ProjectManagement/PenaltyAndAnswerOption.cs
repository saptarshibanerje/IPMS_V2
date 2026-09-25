using IPMS.Domain.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IPMS.Domain.ProjectManagement
{
    /// <summary>
    /// One row on the "Penalty & Answer Choices & Exam Date(s)" tab. Unlike Exam
    /// Phase Detail (which points at a free mix of PostAssignment/Specialization
    /// Ids), this is a one-to-one EXTENSION of a specific ExamPhaseDetail row —
    /// it exists to add Penalty, Answer Choices and Exam Date(s) onto a
    /// combination that was already declared on the Exam Phases tab, rather
    /// than declaring a new combination of its own.
    /// This is the PHASE-level Penalty — always present if set, cannot be left
    /// out, and NOT the same thing as the optional Subject/Section-level
    /// Penalty on TestStructureNode (see NodeConsistencyRules.cs, which checks
    /// the two stay consistent when both are set).
    /// </summary>
    public class PenaltyAndAnswerOption : BatchScopedEntity<long>
    {
        public long ExamPhaseDetailId { get; private set; }   // which Exam Phase combination this belongs to

        public decimal? Penalty { get; private set; }           // e.g. 0.25 for "One-Fourth"
        public int? AnswerChoices { get; private set; }          // e.g. 4 or 5 options
        public bool IsTentative { get; private set; }
        public List<DateTime> ExamDates { get; private set; } = new();

        private PenaltyAndAnswerOption() { }

        public static PenaltyAndAnswerOption Create(long examPhaseDetailId, decimal? penalty, int? answerChoices, bool isTentative, IEnumerable<DateTime> examDates)
        {
            return new PenaltyAndAnswerOption
            {
                ExamPhaseDetailId = examPhaseDetailId,
                Penalty = penalty,
                AnswerChoices = answerChoices,
                IsTentative = isTentative,
                ExamDates = new List<DateTime>(examDates)
            };
        }

        /// <summary> Same Option 3 pattern as ExamPhaseDetail — checked only at Send-to-Review/Approve. </summary>
        public IEnumerable<ValidationMessage> ValidateReferencesExistIn(IReadOnlyCollection<long> knownExamPhaseDetailIds)
        {
            if (!knownExamPhaseDetailIds.Contains(ExamPhaseDetailId))
                yield return new ValidationMessage(Severity.Error,
                    $"A Penalty/Answer-Choice row refers to an Exam Phase combination that no longer exists (Id {ExamPhaseDetailId}).");
        }
    }

}
