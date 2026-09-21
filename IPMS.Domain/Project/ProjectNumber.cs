using IPMS.Domain.Common;
using System.Collections.Generic;

namespace IPMS.Domain.Project
{
    /// <summary>
    /// The running-serial Project Number, e.g. "IBPS/SEL/0001".
    /// OrgAbbr/ProjectTypeAbbr START OUT copied from the ClientOrgSnapshot/PostSnapshot
    /// when this is first created, but AFTER that they live entirely inside the
    /// Project and can be freely edited at any time — editing them here never
    /// touches master data, and later master data edits never touch this.
    ///
    /// A ProjectNumber is also where Posts get declared for this run of the
    /// Project (e.g. "IBPS/SEL/0001" -> Post A under SubOrg A, Post B under
    /// SubOrg A...). Each PostAssignment below is OWNED outright by this one
    /// ProjectNumber — a different ProjectNumber can end up with content that
    /// looks identical, but it will always be a completely separate
    /// PostAssignment with its own Id, never a shared/reused one.
    ///
    /// This ownership stops here, though — downstream tabs (Exam Phases,
    /// Penalty, Sessions, Qualification, Candidate Count...) reference a
    /// PostAssignment/Specialization Id directly, regardless of which
    /// ProjectNumber it was declared under. ProjectNumber is only where a Post
    /// gets ITS Id — it does not cascade any further into the rest of the wizard.
    /// </summary>
    public class ProjectNumber : BatchScopedEntity<long>
    {
        public string OrgAbbr { get; private set; }
        public string ProjectTypeAbbr { get; private set; }
        public string RunningSerial { get; private set; }
        public bool IsConfirmed { get; private set; }

        public string FullNumber => $"{OrgAbbr}/{ProjectTypeAbbr}/{RunningSerial}";

        public List<PostAssignment> PostAssignments { get; private set; } = new();

        private ProjectNumber() { }

        /// <summary>
        /// OrgAbbr/ProjectTypeAbbr here are either taken from the matched master
        /// record, or auto-generated (via StringExtensions.ToAbbreviation) if the
        /// Org/Post was brand new and had no abbreviation yet.
        /// </summary>
        public static ProjectNumber Seed(string orgAbbr, string projectTypeAbbr)
            => new ProjectNumber { OrgAbbr = orgAbbr, ProjectTypeAbbr = projectTypeAbbr, IsConfirmed = false };

        public void UpdateOrgAbbr(string newAbbr) => OrgAbbr = newAbbr;
        public void UpdateProjectTypeAbbr(string newAbbr) => ProjectTypeAbbr = newAbbr;

        /// <summary> The actual "confirmation" moment — locks in the running serial number. </summary>
        public void ConfirmWithSerial(string runningSerial)
        {
            RunningSerial = runningSerial;
            IsConfirmed = true;
        }

        /// <summary>
        /// Declares a new Post under this ProjectNumber. Always creates a BRAND
        /// NEW PostAssignment, even if an identical-looking one already exists
        /// under a different ProjectNumber — no sharing, ever (confirmed rule).
        /// The new PostAssignment starts life in the SAME Batch as this
        /// ProjectNumber — no separate assignment needed by the caller.
        /// </summary>
        public PostAssignment AddPostAssignment(PostSnapshot post, SubOrganizationSnapshot subOrganization = null)
        {
            var assignment = PostAssignment.Create(post, subOrganization);
            assignment.AssignBatch(BatchId);
            PostAssignments.Add(assignment);
            return assignment;
        }
    }
}
