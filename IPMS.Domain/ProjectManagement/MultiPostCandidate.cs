using IPMS.Domain.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IPMS.Domain.ProjectManagement
{
    /// <summary>
    /// One row on the "Multi-Post Configuration" tab — a group of
    /// PostAssignments that a single candidate is allowed to apply to
    /// TOGETHER, when the Project allows multi-post applications. Same
    /// free-mix rule as everywhere else: the Ids in one group can come from
    /// different ProjectNumbers.
    /// </summary>
    public class MultiPostCandidate : BatchScopedEntity<long>
    {
        public List<long> PostAssignmentIds { get; private set; } = new();

        private MultiPostCandidate() { }

        public static MultiPostCandidate Create(IEnumerable<long> postAssignmentIds)
            => new MultiPostCandidate { PostAssignmentIds = new List<long>(postAssignmentIds) };

        public IEnumerable<ValidationMessage> ValidateReferencesExistIn(IReadOnlyCollection<long> knownPostAssignmentIds)
        {
            foreach (var id in PostAssignmentIds)
                if (!knownPostAssignmentIds.Contains(id))
                    yield return new ValidationMessage(Severity.Error,
                        $"A Multi-Post combination refers to a Post that no longer exists (Id {id}).");
        }
    }

}
