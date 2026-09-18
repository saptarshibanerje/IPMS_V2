using System.Collections.Generic;
using System.Linq;
using IPMS.Domain.Common;
using IPMS.Domain.Project.ExamStructure;

namespace IPMS.Domain.Project
{
    /// <summary>
    /// THE AGGREGATE ROOT. This is the ONE class the rest of the application is
    /// allowed to load and save directly (through IProjectRepository — not shown
    /// in this skeleton yet). Everything else (Batches, PostSpecializations,
    /// ExamStructure nodes...) is reached only THROUGH this class — nothing
    /// outside is allowed to load a Batch or a TestStructureNode on its own.
    /// </summary>
    public class Project : AggregateRoot<long>
    {
        public long PermanentProjectIdentificationNo { get; private set; }  // survives forever, across every batch/revision
        public string DivisionOwner { get; private set; }                     // which Division currently owns/edits this

        public List<Batch> Batches { get; private set; } = new();
        public List<Revision> Revisions { get; private set; } = new();
        public List<PostSpecialization> PostSpecializations { get; private set; } = new();
        public List<TestStructureNode> ExamStructureSubjects { get; private set; } = new();   // top-level nodes only; each has its own Children
        public List<ProjectNumber> ProjectNumbers { get; private set; } = new();

        // Every other tab's collection (ExamPhaseDetail, PenaltyAndAnswerOption,
        // DateWiseSession, CandidateCount, CoOrdinatorDetail, UploadDocument, etc.)
        // follows the exact same simple pattern as PostSpecialization — a plain
        // Entity<long> that carries a List<long> of PostSpecializationIds it
        // refers to. Left out of this first skeleton so it stays easy to review;
        // happy to add them once you've looked this over.

        public Batch CurrentBatch => Batches.Single(b => b.Status != BatchStatus.Approved);

        private Project() { }

        /// <summary> Called once, when a Division first creates a brand new Project. </summary>
        public static Project CreateNew(long permanentProjectId, string divisionOwner, string createdBy)
        {
            var project = new Project
            {
                PermanentProjectIdentificationNo = permanentProjectId,
                DivisionOwner = divisionOwner
            };
            project.Batches.Add(Batch.CreateInitial(project.Id, createdBy));
            // RaiseEvent(...) could go here later, e.g. a "ProjectCreated" event, for audit logging.
            return project;
        }

        /// <summary>
        /// Runs EVERY validation rule across the WHOLE project — every child
        /// collection, every node in the exam structure tree — and collects every
        /// problem found (both blocking Errors and advisory Warnings) into one
        /// list. This is the SAME method used both when the maker just wants a
        /// "sanity check" prompt, and when Send-to-Review / Approve needs to
        /// actually decide whether to allow the transition.
        /// </summary>
        public ValidationResult ValidateCurrentState()
        {
            var result = new ValidationResult();

            // Cross-tab referential integrity (Option 3) would loop over
            // ExamPhaseDetail etc. here, checking every PostSpecializationIds
            // reference still exists in PostSpecializations — added once those
            // collections are written.

            // Exam structure — walks every Subject/Section/SubSection node and
            // checks the cross-level Penalty consistency rule (and anything else
            // added to NodeConsistencyRules.cs later).
            foreach (var subject in ExamStructureSubjects)
                foreach (var node in subject.SelfAndDescendants())
                    result.AddRange(NodeConsistencyRules.ValidateAll(node));

            return result;
        }

        /// <summary>
        /// Maker clicks "Send to Review". Only blocked by real Errors — Warnings
        /// (like a Penalty conflict) never stop this; the maker just gets shown
        /// them as an "are you sure?" prompt by the calling Application Service.
        /// </summary>
        public ValidationResult SendForReview(string userId)
        {
            var result = ValidateCurrentState();
            if (!result.HasBlockingErrors)
                CurrentBatch.SendToReview(userId);
            return result;
        }

        public ValidationResult ForwardToApproval(string reviewerId)
        {
            var result = ValidateCurrentState();
            if (!result.HasBlockingErrors)
                CurrentBatch.ForwardToApproval(reviewerId);
            return result;
        }

        public void SendBackToMaker(string byUserId, string remarks)
            => CurrentBatch.SendBackToMaker(byUserId, remarks);

        /// <summary>
        /// Final approval. Freezes the current batch forever. The user can later
        /// click "Edit" (a follow-up method, not shown yet) which spawns a brand
        /// new Batch off this one.
        /// </summary>
        public ValidationResult Approve(string approverId)
        {
            var result = ValidateCurrentState();
            if (!result.HasBlockingErrors)
                CurrentBatch.Approve(approverId);
            return result;
        }

        /// <summary>
        /// "Send Intimation" — publishes the current Approved batch as a Revision,
        /// visible to OTHER divisions (who never see Batches, only Revisions).
        /// No approval gate of its own — just requires an Approved batch that
        /// hasn't already been published.
        /// </summary>
        public Revision SendIntimation(string userId)
        {
            var approvedBatch = Batches
                .Where(b => b.Status == BatchStatus.Approved)
                .OrderByDescending(b => b.Id)
                .FirstOrDefault(b => Revisions.All(r => r.SourceBatchId != b.Id));

            if (approvedBatch is null)
                throw new System.InvalidOperationException("No newly-approved batch is available to publish as a revision.");

            var revision = Revision.CreateFrom(approvedBatch, Revisions, userId);
            Revisions.Add(revision);
            return revision;
        }
    }
}
