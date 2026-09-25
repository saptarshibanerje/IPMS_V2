using IPMS.Domain.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IPMS.Domain.ProjectManagement
{
    /// <summary>
    /// One row on the "Sessions" tab — assigns one or more Session names (e.g.
    /// "Morning", "Afternoon", or a custom one) to a specific exam date that
    /// was declared on the Penalty & Answer Choices & Exam Date(s) tab.
    /// Points at a PenaltyAndAnswerOption row rather than directly at an
    /// ExamPhaseDetail, since that's where the actual exam date(s) live.
    /// </summary>
    public class DateWiseSession : BatchScopedEntity<long>
    {
        public long PenaltyAndAnswerOptionId { get; private set; }

        // Session names come from the master pool (same frozen-snapshot pattern
        // as everything else picked from a typeahead) — a date can have more
        // than one session (e.g. both a Morning and an Afternoon sitting).
        public List<PoolSnapshot> SessionNames { get; private set; } = new();

        private DateWiseSession() { }

        public static DateWiseSession Create(long penaltyAndAnswerOptionId, IEnumerable<PoolSnapshot> sessionNames)
        {
            return new DateWiseSession
            {
                PenaltyAndAnswerOptionId = penaltyAndAnswerOptionId,
                SessionNames = new List<PoolSnapshot>(sessionNames)
            };
        }

        public IEnumerable<ValidationMessage> ValidateReferencesExistIn(IReadOnlyCollection<long> knownPenaltyAndAnswerOptionIds)
        {
            if (!knownPenaltyAndAnswerOptionIds.Contains(PenaltyAndAnswerOptionId))
                yield return new ValidationMessage(Severity.Error,
                    $"A Session row refers to a Penalty/Exam-Date row that no longer exists (Id {PenaltyAndAnswerOptionId}).");
        }
    }

}
