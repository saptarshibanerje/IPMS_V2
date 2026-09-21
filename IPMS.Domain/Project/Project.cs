using System;
using System.Collections.Generic;
using System.Linq;
using IPMS.Domain.Common;
using IPMS.Domain.Project.ExamStructure;

namespace IPMS.Domain.Project
{
    /// <summary>
    /// THE AGGREGATE ROOT. This is the ONE class the rest of the application is
    /// allowed to load and save directly (through IProjectRepository — not shown
    /// in this skeleton yet). Everything else (Batches, ProjectNumbers,
    /// PostAssignments, ExamStructure nodes...) is reached only THROUGH this
    /// class — nothing outside is allowed to load a Batch or a
    /// TestStructureNode on its own.
    /// </summary>
    public class Project : AggregateRoot<long>
    {
        public long PermanentProjectIdentificationNo { get; private set; }  // survives forever, across every batch/revision
        public string DivisionOwner { get; private set; }                     // which Division currently owns/edits this

        public List<Batch> Batches { get; private set; } = new();
        public List<Revision> Revisions { get; private set; } = new();
        public List<TestStructureNode> ExamStructureSubjects { get; private set; } = new();   // top-level nodes only; each has its own Children

        // ProjectNumber owns its own PostAssignments (and each PostAssignment
        // owns its own Specializations) — see ProjectNumber.cs and
        // PostAssignment.cs. Posts/Specializations are declared HERE, under a
        // ProjectNumber, but every downstream tab references a
        // PostAssignment/Specialization Id directly, unscoped by which
        // ProjectNumber it came from — ownership stops at ProjectNumber, it
        // does not cascade any further into Phase/Penalty/Sessions/etc.
        public List<ProjectNumber> ProjectNumbers { get; private set; } = new();

        public List<ExamPhaseDetail> ExamPhaseDetails { get; private set; } = new();

        // Every OTHER tab's collection (PenaltyAndAnswerOption, DateWiseSession,
        // CandidateCount, CoOrdinatorDetail, UploadDocument, etc.) follows the
        // exact same simple pattern as ExamPhaseDetail above — a plain
        // BatchScopedEntity<long> that carries a List<long> of PostAssignment/
        // Specialization Ids it refers to. Left out of this first skeleton so
        // it stays easy to review; happy to add them once you've looked this over.

        // The Batch currently "in play" for this Project — either still being
        // edited/reviewed/approved, OR the most recently Approved one if
        // nobody has clicked Edit since. NOT the same as "the one non-Approved
        // batch", because right after an Approval there may be ZERO non-Approved
        // batches at all — ordering by Id and taking the highest one covers
        // BOTH situations correctly.
        public Batch LatestBatch => Batches.OrderByDescending(b => b.Id).First();

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
        /// Declares a new ProjectNumber, e.g. "IBPS/SEL/0001". Automatically
        /// starts life in whichever Batch is currently active — the caller
        /// never has to think about BatchId directly.
        /// </summary>
        public ProjectNumber AddProjectNumber(string orgAbbr, string projectTypeAbbr)
        {
            var projectNumber = ProjectNumber.Seed(orgAbbr, projectTypeAbbr);
            projectNumber.AssignBatch(LatestBatch.Id);
            ProjectNumbers.Add(projectNumber);
            return projectNumber;
        }

        /// <summary> Adds a new Exam Phase combination, e.g. "Post A + Post D -> Single Stage -> Objective". </summary>
        public ExamPhaseDetail AddExamPhaseDetail(
            IEnumerable<long> postAssignmentIds, IEnumerable<long> specializationIds,
            string examStage, IEnumerable<string> testTypes)
        {
            var phase = ExamPhaseDetail.Create(postAssignmentIds, specializationIds, examStage, testTypes);
            phase.AssignBatch(LatestBatch.Id);
            ExamPhaseDetails.Add(phase);
            return phase;
        }

        /// <summary> Adds a new top-level Subject to the exam structure tree (see TestStructureNode.cs). </summary>
        public TestStructureNode AddExamStructureSubject(string name, long nodeTypeId, int sequenceNo)
        {
            var subject = TestStructureNode.CreateSubject(name, nodeTypeId, sequenceNo);
            subject.AssignBatch(LatestBatch.Id);
            ExamStructureSubjects.Add(subject);
            return subject;
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

            // Gather every PostAssignment/Specialization Id that exists ANYWHERE
            // across ALL ProjectNumbers — not scoped to just one. This is what
            // makes "Post A (from ProjectNumber 1) + Post D (from ProjectNumber
            // 2)" a perfectly valid Exam Phase combination: as far as this check
            // is concerned, there's just one flat pool of known Ids, regardless
            // of which ProjectNumber originally declared them.
            var knownPostAssignmentIds = ProjectNumbers
                .SelectMany(pn => pn.PostAssignments)
                .Select(pa => pa.Id)
                .ToList();

            var knownSpecializationIds = ProjectNumbers
                .SelectMany(pn => pn.PostAssignments)
                .SelectMany(pa => pa.Specializations)
                .Select(s => s.Id)
                .ToList();

            // Cross-tab referential integrity (Option 3) — an orphaned
            // reference is a blocking Error, but ONLY surfaced here, at
            // Send-to-Review / Approve time — never mid-edit.
            foreach (var phase in ExamPhaseDetails)
                result.AddRange(phase.ValidateReferencesExistIn(knownPostAssignmentIds, knownSpecializationIds));

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
                LatestBatch.SendToReview(userId);
            return result;
        }

        public ValidationResult ForwardToApproval(string reviewerId)
        {
            var result = ValidateCurrentState();
            if (!result.HasBlockingErrors)
                LatestBatch.ForwardToApproval(reviewerId);
            return result;
        }

        public void SendBackToMaker(string byUserId, string remarks)
            => LatestBatch.SendBackToMaker(byUserId, remarks);

        /// <summary>
        /// Final approval. Freezes the current batch forever. The user can later
        /// click "Edit", which calls CreateFollowUpBatch below — spawning a
        /// brand new Batch and carrying this content forward onto it.
        /// </summary>
        public ValidationResult Approve(string approverId)
        {
            var result = ValidateCurrentState();
            if (!result.HasBlockingErrors)
                LatestBatch.Approve(approverId);
            return result;
        }

        /// <summary>
        /// User clicks "Edit" on an Approved batch. Creates a brand-new Batch,
        /// then CARRIES FORWARD every not-soft-deleted content row onto it by
        /// repointing each row's BatchId — NOT by cloning rows with new Ids.
        /// Because a row's own Id never changes, every other row that
        /// references it (Exam Phase pointing at a PostAssignment, for example)
        /// stays correct automatically. Soft-deleted rows are left exactly
        /// where they are, still pointing at the old (now-historical) Batch —
        /// there's nothing to carry forward for something that was deleted.
        /// </summary>
        public Batch CreateFollowUpBatch(string createdBy)
        {
            var previousBatch = LatestBatch;
            if (previousBatch.Status != BatchStatus.Approved)
                throw new InvalidOperationException("Can only branch a new batch off an Approved one.");

            var newBatch = Batch.CreateFollowUp(previousBatch, createdBy);
            Batches.Add(newBatch);

            var carriedForwardCount = 0;

            foreach (var projectNumber in ProjectNumbers.Where(x => !x.IsDelete))
            {
                projectNumber.AssignBatch(newBatch.Id);
                carriedForwardCount++;

                foreach (var postAssignment in projectNumber.PostAssignments.Where(x => !x.IsDelete))
                {
                    postAssignment.AssignBatch(newBatch.Id);
                    carriedForwardCount++;

                    foreach (var specialization in postAssignment.Specializations.Where(x => !x.IsDelete))
                    {
                        specialization.AssignBatch(newBatch.Id);
                        carriedForwardCount++;
                    }
                }
            }

            foreach (var phase in ExamPhaseDetails.Where(x => !x.IsDelete))
            {
                phase.AssignBatch(newBatch.Id);
                carriedForwardCount++;
            }

            foreach (var subject in ExamStructureSubjects.Where(x => !x.IsDelete))
                foreach (var node in subject.SelfAndDescendants().Where(x => !x.IsDelete))
                {
                    node.AssignBatch(newBatch.Id);
                    carriedForwardCount++;
                }

            // ...the exact same three-line pattern repeats for every remaining
            // tab collection (PenaltyAndAnswerOption, DateWiseSession,
            // CandidateCount, CoOrdinatorDetail, UploadDocument, etc.) once
            // they're written — nothing new to design, just more of the same.

            // ONE summary event, not one per row — this is what lets the audit
            // log record "Batch 1005 created, carrying forward 200 rows"
            // instead of 200 near-identical entries burying the changes that
            // actually matter.
            RaiseEvent(new BatchFollowUpCreated(
                PermanentProjectIdentificationNo, previousBatch.Id, newBatch.Id, carriedForwardCount, createdBy));

            return newBatch;
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
                throw new InvalidOperationException("No newly-approved batch is available to publish as a revision.");

            var revision = Revision.CreateFrom(approvedBatch, Revisions, userId);
            Revisions.Add(revision);
            return revision;
        }
    }
}