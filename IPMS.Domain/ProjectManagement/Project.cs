using System;
using System.Collections.Generic;
using System.Linq;
using IPMS.Domain.Common;
using IPMS.Domain.ProjectManagement.ExamStructure;

namespace IPMS.Domain.ProjectManagement
{
    /// <summary>
    /// THE AGGREGATE ROOT. This is the ONE class the rest of the application is
    /// allowed to load and save directly (through IProjectRepository — not shown
    /// in this skeleton yet). Everything else (Batches, ProjectNumbers,
    /// PostAssignments, ExamStructure nodes, and every tab collection below) is
    /// reached only THROUGH this class — nothing outside is allowed to load a
    /// Batch or a TestStructureNode on its own.
    /// </summary>
    public class Project : AggregateRoot<long>
    {
        // NOTE: there is no separate "PermanentProjectIdentificationNo" field.
        // Earlier in this design we confirmed PPIN and the Project's own Id
        // ARE the same thing — so it's just `Id` (inherited from Entity<long>),
        // assigned by the database on save, same as every other entity here.
        public string DivisionOwner { get; private set; }   // which Division currently owns/edits this — shown on screen as "Project Authority"

        // ---- Foundational details (Initial Information tab, part 1) ----------
        // These 5 live directly on PROJECT, not inside a Batch — because the
        // rule governing them isn't "editable while Created, frozen while
        // Approved" like everything else. It's a ONE-TIME, PERMANENT lock:
        // freely editable up until the very FIRST batch this Project ever has
        // is Approved, then locked forever, even across every later batch.
        // See FoundationalDetailsAreLocked and SetFoundationalDetails below.
        public PoolSnapshot ClientCategory { get; private set; }
        public ClientOrgSnapshot ClientOrganization { get; private set; }
        public ProjectTypeSnapshot ProjectType { get; private set; }
        public string ProjectTitle { get; private set; }
        public string ShortDescription { get; private set; }

        /// <summary>
        /// True forever, the moment ANY batch on this Project has ever reached
        /// Approved — not just the current one. Once true, it never becomes
        /// false again, even if later batches cycle back through
        /// Created/Review/etc. for everything else.
        /// </summary>
        public bool FoundationalDetailsAreLocked => Batches.Any(b => b.Status == BatchStatus.Approved);

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
        public List<PenaltyAndAnswerOption> PenaltyAndAnswerOptions { get; private set; } = new();
        public List<DateWiseSession> DateWiseSessions { get; private set; } = new();
        public List<EducationQualification> EducationQualifications { get; private set; } = new();
        public List<MultiPostCandidate> MultiPostCandidates { get; private set; } = new();
        public List<CandidateCount> CandidateCounts { get; private set; } = new();
        public List<CoOrdinatorDetail> CoOrdinatorDetails { get; private set; } = new();
        public List<UploadDocument> UploadDocuments { get; private set; } = new();

        // Only ONE per batch, not a list — see ExplanationNote.cs.
        public ExplanationNote Note { get; private set; }

        // Same single-instance-per-batch idea, for the OTHER Initial
        // Information tab fields — the ones that stay freely editable at any
        // time (unlike the 5 locked ones just above). See ProjectInitialDetails.cs.
        public ProjectInitialDetails InitialDetails { get; private set; }

        // The Batch currently "in play" for this Project — either still being
        // edited/reviewed/approved, OR the most recently Approved one if
        // nobody has clicked Edit since. NOT the same as "the one non-Approved
        // batch", because right after an Approval there may be ZERO non-Approved
        // batches at all — ordering by Id and taking the highest one covers
        // BOTH situations correctly.
        public Batch LatestBatch => Batches.OrderByDescending(b => b.Id).First();

        private Project() { }

        /// <summary>
        /// Called once, when a Division first creates a brand new Project.
        /// Deliberately does NOT take an Id/PPIN parameter — a brand new
        /// Project is "transient" (see Entity.IsTransient()) until it's
        /// actually saved and the database assigns its real Id. Anything that
        /// needs the real PPIN (generating a document, showing it on screen)
        /// has to happen AFTER the first save, never before.
        /// </summary>
        public static Project CreateNew(string divisionOwner, string createdBy)
        {
            var project = new Project { DivisionOwner = divisionOwner };
            project.Batches.Add(Batch.CreateInitial(project.Id, createdBy));
            // RaiseEvent(...) could go here later, e.g. a "ProjectCreated" event, for audit logging.
            return project;
        }

        // ---- Creation helpers -------------------------------------------------
        // Every one of these does the SAME two things: build the child entity,
        // then AssignBatch(LatestBatch.Id) so it's born already pointing at
        // whichever Batch is currently active. The caller never has to think
        // about BatchId directly — Project handles it consistently everywhere.

        /// <summary>
        /// Declares a new ProjectNumber, e.g. "IBPS/SEL/0001". OrgAbbr and
        /// ProjectTypeAbbr are no longer passed in by the caller — they're
        /// pulled directly from this Project's own (locked) ClientOrganization
        /// and ProjectType, since those now live right here on Project.
        /// SetFoundationalDetails must have been called first.
        /// </summary>
        public ProjectNumber AddProjectNumber()
        {
            if (ClientOrganization is null || ProjectType is null)
                throw new InvalidOperationException(
                    "Client Organization and Project Type must be set (see SetFoundationalDetails) before declaring a Project Number.");

            var projectNumber = ProjectNumber.Seed(ClientOrganization.OrgAbbr, ProjectType.Abbr);
            projectNumber.AssignBatch(LatestBatch.Id);
            ProjectNumbers.Add(projectNumber);
            return projectNumber;
        }

        public ExamPhaseDetail AddExamPhaseDetail(
            IEnumerable<long> postAssignmentIds, IEnumerable<long> specializationIds,
            string examStage, IEnumerable<string> testTypes)
        {
            var phase = ExamPhaseDetail.Create(postAssignmentIds, specializationIds, examStage, testTypes);
            phase.AssignBatch(LatestBatch.Id);
            ExamPhaseDetails.Add(phase);
            return phase;
        }

        public PenaltyAndAnswerOption AddPenaltyAndAnswerOption(
            long examPhaseDetailId, decimal? penalty, int? answerChoices, bool isTentative, IEnumerable<DateTime> examDates)
        {
            var penaltyRow = PenaltyAndAnswerOption.Create(examPhaseDetailId, penalty, answerChoices, isTentative, examDates);
            penaltyRow.AssignBatch(LatestBatch.Id);
            PenaltyAndAnswerOptions.Add(penaltyRow);
            return penaltyRow;
        }

        public DateWiseSession AddDateWiseSession(long penaltyAndAnswerOptionId, IEnumerable<PoolSnapshot> sessionNames)
        {
            var session = DateWiseSession.Create(penaltyAndAnswerOptionId, sessionNames);
            session.AssignBatch(LatestBatch.Id);
            DateWiseSessions.Add(session);
            return session;
        }

        public EducationQualification AddEducationQualification(
            IEnumerable<long> postAssignmentIds, IEnumerable<long> specializationIds,
            string qualificationText, string experienceText)
        {
            var qualification = EducationQualification.Create(postAssignmentIds, specializationIds, qualificationText, experienceText);
            qualification.AssignBatch(LatestBatch.Id);
            EducationQualifications.Add(qualification);
            return qualification;
        }

        public MultiPostCandidate AddMultiPostCandidate(IEnumerable<long> postAssignmentIds)
        {
            var combination = MultiPostCandidate.Create(postAssignmentIds);
            combination.AssignBatch(LatestBatch.Id);
            MultiPostCandidates.Add(combination);
            return combination;
        }

        public CandidateCount AddCandidateCount(string stage, long? postAssignmentId, long? specializationId, int count)
        {
            var candidateCount = CandidateCount.Create(stage, postAssignmentId, specializationId, count);
            candidateCount.AssignBatch(LatestBatch.Id);
            CandidateCounts.Add(candidateCount);
            return candidateCount;
        }

        public CoOrdinatorDetail AddCoOrdinatorDetail(string divisionName, string name, string email, string phoneNumber)
        {
            var coOrdinator = CoOrdinatorDetail.Create(divisionName, name, email, phoneNumber);
            coOrdinator.AssignBatch(LatestBatch.Id);
            CoOrdinatorDetails.Add(coOrdinator);
            return coOrdinator;
        }

        public UploadDocument AddUploadDocument(string fileName, string fileType, long fileSizeBytes, string storagePath)
        {
            var document = UploadDocument.Create(fileName, fileType, fileSizeBytes, storagePath);
            document.AssignBatch(LatestBatch.Id);
            UploadDocuments.Add(document);
            return document;
        }

        public TestStructureNode AddExamStructureSubject(string name, long nodeTypeId, int sequenceNo)
        {
            var subject = TestStructureNode.CreateSubject(name, nodeTypeId, sequenceNo);
            subject.AssignBatch(LatestBatch.Id);
            ExamStructureSubjects.Add(subject);
            return subject;
        }

        /// <summary> Creates the Note the first time, or updates it in place on later saves — there's only ever one per batch. </summary>
        public void SetNote(string text)
        {
            if (Note is null)
            {
                Note = ExplanationNote.Create(text);
                Note.AssignBatch(LatestBatch.Id);
            }
            else
            {
                Note.UpdateText(text);
            }
        }

        /// <summary>
        /// Saves the "Initial Information" tab's freely-editable fields —
        /// creates ProjectInitialDetails the first time, updates it in place
        /// afterward. No lock check here; these can change any time.
        /// </summary>
        public void SetInitialDetails(
            string contactPersonName, string contactPersonEmail, string contactNumber,
            PoolSnapshot rollNumberStructure, List<PoolSnapshot> additionalServices,
            List<PoolSnapshot> preExamActivityHandledBy, string dispatchMode,
            bool handwritingSampleRequired, bool isMultipostAllowed,
            int numberOfProjectNumbers, List<DateTime> resultToBeSharedOn)
        {
            if (InitialDetails is null)
            {
                InitialDetails = ProjectInitialDetails.Create();
                InitialDetails.AssignBatch(LatestBatch.Id);
            }

            InitialDetails.Update(
                contactPersonName, contactPersonEmail, contactNumber,
                rollNumberStructure, additionalServices, preExamActivityHandledBy,
                dispatchMode, handwritingSampleRequired, isMultipostAllowed,
                numberOfProjectNumbers, resultToBeSharedOn);
        }

        /// <summary>
        /// Saves Client Category / Client Organization / Project Type /
        /// Project Title / Short Description — but ONLY before the Project's
        /// very first batch has ever been approved. After that, this throws:
        /// these 5 fields are permanently locked, by design, from that point on.
        /// </summary>
        public void SetFoundationalDetails(
            PoolSnapshot clientCategory, ClientOrgSnapshot clientOrganization,
            ProjectTypeSnapshot projectType, string projectTitle, string shortDescription)
        {
            if (FoundationalDetailsAreLocked)
                throw new InvalidOperationException(
                    "Client Category, Client Organization, Project Type, Project Title and " +
                    "Short Description can no longer be changed — this project's first batch has already been approved.");

            ClientCategory = clientCategory;
            ClientOrganization = clientOrganization;
            ProjectType = projectType;
            ProjectTitle = projectTitle;
            ShortDescription = shortDescription;
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

            var knownExamPhaseDetailIds = ExamPhaseDetails.Select(p => p.Id).ToList();
            var knownPenaltyAndAnswerOptionIds = PenaltyAndAnswerOptions.Select(p => p.Id).ToList();

            // Cross-tab referential integrity (Option 3) — an orphaned
            // reference is a blocking Error, but ONLY surfaced here, at
            // Send-to-Review / Approve time — never mid-edit.
            foreach (var phase in ExamPhaseDetails)
                result.AddRange(phase.ValidateReferencesExistIn(knownPostAssignmentIds, knownSpecializationIds));

            foreach (var penalty in PenaltyAndAnswerOptions)
                result.AddRange(penalty.ValidateReferencesExistIn(knownExamPhaseDetailIds));

            foreach (var session in DateWiseSessions)
                result.AddRange(session.ValidateReferencesExistIn(knownPenaltyAndAnswerOptionIds));

            foreach (var qualification in EducationQualifications)
                result.AddRange(qualification.ValidateReferencesExistIn(knownPostAssignmentIds, knownSpecializationIds));

            foreach (var multiPost in MultiPostCandidates)
                result.AddRange(multiPost.ValidateReferencesExistIn(knownPostAssignmentIds));

            foreach (var count in CandidateCounts)
                result.AddRange(count.ValidateReferencesExistIn(knownPostAssignmentIds, knownSpecializationIds));

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

            foreach (var penalty in PenaltyAndAnswerOptions.Where(x => !x.IsDelete))
            {
                penalty.AssignBatch(newBatch.Id);
                carriedForwardCount++;
            }

            foreach (var session in DateWiseSessions.Where(x => !x.IsDelete))
            {
                session.AssignBatch(newBatch.Id);
                carriedForwardCount++;
            }

            foreach (var qualification in EducationQualifications.Where(x => !x.IsDelete))
            {
                qualification.AssignBatch(newBatch.Id);
                carriedForwardCount++;
            }

            foreach (var multiPost in MultiPostCandidates.Where(x => !x.IsDelete))
            {
                multiPost.AssignBatch(newBatch.Id);
                carriedForwardCount++;
            }

            foreach (var count in CandidateCounts.Where(x => !x.IsDelete))
            {
                count.AssignBatch(newBatch.Id);
                carriedForwardCount++;
            }

            foreach (var coOrdinator in CoOrdinatorDetails.Where(x => !x.IsDelete))
            {
                coOrdinator.AssignBatch(newBatch.Id);
                carriedForwardCount++;
            }

            foreach (var document in UploadDocuments.Where(x => !x.IsDelete))
            {
                document.AssignBatch(newBatch.Id);
                carriedForwardCount++;
            }

            if (Note is not null && !Note.IsDelete)
            {
                Note.AssignBatch(newBatch.Id);
                carriedForwardCount++;
            }

            if (InitialDetails is not null && !InitialDetails.IsDelete)
            {
                InitialDetails.AssignBatch(newBatch.Id);
                carriedForwardCount++;
            }
            // Note: ClientCategory / ClientOrganization / ProjectType / ProjectTitle /
            // ShortDescription are NOT part of this walk — they live directly on
            // Project itself (not inside any Batch), so there's nothing to
            // repoint for them. They're already locked by this point anyway,
            // since CreateFollowUpBatch can only run after the first Approval.

            foreach (var subject in ExamStructureSubjects.Where(x => !x.IsDelete))
                foreach (var node in subject.SelfAndDescendants().Where(x => !x.IsDelete))
                {
                    node.AssignBatch(newBatch.Id);
                    carriedForwardCount++;
                }

            // ONE summary event, not one per row — this is what lets the audit
            // log record "Batch 1005 created, carrying forward 200 rows"
            // instead of 200 near-identical entries burying the changes that
            // actually matter.
            RaiseEvent(new BatchFollowUpCreated(
                Id, previousBatch.Id, newBatch.Id, carriedForwardCount, createdBy));

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