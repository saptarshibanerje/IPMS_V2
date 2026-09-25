using IPMS.Domain.Common;
using System;
using System.Collections.Generic;
using System.Linq;

namespace IPMS.Domain.ProjectManagement
{
    /// <summary>
    /// One row on the "Education Qualifications/Specializations" tab — free-text
    /// qualification and experience requirements, attached to a combination of
    /// PostAssignment/Specialization Ids (same free-mix rule as ExamPhaseDetail
    /// — the combination can draw from any ProjectNumber, not just one).
    /// </summary>
    public class EducationQualification : BatchScopedEntity<long>
    {
        public List<long> PostAssignmentIds { get; private set; } = new();
        public List<long> SpecializationIds { get; private set; } = new();

        public string QualificationText { get; private set; }
        public string ExperienceText { get; private set; }

        private EducationQualification() { }

        public static EducationQualification Create(
            IEnumerable<long> postAssignmentIds, IEnumerable<long> specializationIds,
            string qualificationText, string experienceText)
        {
            return new EducationQualification
            {
                PostAssignmentIds = new List<long>(postAssignmentIds),
                SpecializationIds = new List<long>(specializationIds),
                QualificationText = qualificationText,
                ExperienceText = experienceText
            };
        }

        public IEnumerable<ValidationMessage> ValidateReferencesExistIn(
            IReadOnlyCollection<long> knownPostAssignmentIds, IReadOnlyCollection<long> knownSpecializationIds)
        {
            foreach (var id in PostAssignmentIds)
                if (!knownPostAssignmentIds.Contains(id))
                    yield return new ValidationMessage(Severity.Error,
                        $"Education Qualification refers to a Post that no longer exists (Id {id}).");

            foreach (var id in SpecializationIds)
                if (!knownSpecializationIds.Contains(id))
                    yield return new ValidationMessage(Severity.Error,
                        $"Education Qualification refers to a Specialization that no longer exists (Id {id}).");
        }
    }

}
