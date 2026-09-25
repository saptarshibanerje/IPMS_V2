using System;
using IPMS.Domain.Common;

namespace IPMS.Domain.ProjectManagement
{
    /// <summary>
    /// A BATCH = one full edit-review-approve cycle. Think of it as a "working
    /// version" of the whole Project while someone is actively editing it. Only
    /// ONE Batch is ever "live" (not yet Approved) per Project at a time — every
    /// other Batch belonging to the Project is already Approved history.
    ///
    /// LastApprovedSnapshotJson = a FROZEN copy of the data as it stood at the
    ///                            PREVIOUS approval (null for the very first Batch
    ///                            a Project ever has — there's nothing before it).
    /// UpdatedSnapshotJson      = the CURRENT live working data, re-saved every
    ///                            time the maker edits a tab while this Batch is
    ///                            still in the "Created" state.
    ///
    /// Comparing these two fields is ALL we ever need to show "what changed in
    /// this Batch" — we never have to compare against anything further back.
    /// </summary>
    public class Batch : Entity<long>
    {
        public long ProjectIdentificationNo { get; private set; }
        public BatchStatus Status { get; private set; }

        public string LastApprovedSnapshotJson { get; private set; }
        public string UpdatedSnapshotJson { get; private set; }

        private Batch() { } // EF Core needs a parameterless constructor — never call this directly from app code.

        /// <summary>
        /// Creates the very FIRST batch of a brand new Project. There's nothing to
        /// compare against yet, so LastApprovedSnapshot starts out empty.
        /// </summary>
        public static Batch CreateInitial(long projectId, string createdBy)
        {
            return new Batch
            {
                ProjectIdentificationNo = projectId,
                Status = BatchStatus.Created,
                LastApprovedSnapshotJson = null,
                CreatedBy = createdBy,
                CreatedDate = DateTime.UtcNow
            };
        }

        /// <summary>
        /// Called when the user clicks "Edit" on an already-Approved batch. We do
        /// NOT reopen the old batch — we spawn a brand new one, seeding its
        /// "LastApproved" baseline from the old batch's final approved data.
        /// </summary>
        public static Batch CreateFollowUp(Batch previouslyApprovedBatch, string createdBy)
        {
            if (previouslyApprovedBatch.Status != BatchStatus.Approved)
                throw new InvalidOperationException("Can only branch a new batch off an Approved one.");

            return new Batch
            {
                ProjectIdentificationNo = previouslyApprovedBatch.ProjectIdentificationNo,
                Status = BatchStatus.Created,
                LastApprovedSnapshotJson = previouslyApprovedBatch.UpdatedSnapshotJson, // old "current" becomes new "baseline"
                CreatedBy = createdBy,
                CreatedDate = DateTime.UtcNow
            };
        }

        /// <summary> Maker saves a tab. Only allowed while the batch is still editable. </summary>
        public void SaveWorkingData(string newSnapshotJson, string updatedBy)
        {
            if (Status != BatchStatus.Created)
                throw new InvalidOperationException("This batch is frozen right now and cannot be edited.");

            UpdatedSnapshotJson = newSnapshotJson;
            UpdatedBy = updatedBy;
            UpdatedDate = DateTime.UtcNow;
        }

        /// <summary> Maker clicks "Send to Review". Freezes editing on this Batch. </summary>
        public void SendToReview(string userId)
        {
            if (Status != BatchStatus.Created)
                throw new InvalidOperationException("Only an editable batch can be sent to review.");
            Status = BatchStatus.UnderReview;
        }

        /// <summary> Reviewer looked at the changes and is happy — forwards it to the final approver. </summary>
        public void ForwardToApproval(string reviewerId)
        {
            if (Status != BatchStatus.UnderReview)
                throw new InvalidOperationException("Only a batch under review can be forwarded to approval.");
            Status = BatchStatus.UnderApproval;
        }

        /// <summary>
        /// Reviewer OR Approver is not happy — sends it back to the maker.
        /// Unfreezes the SAME batch (a rejection never spawns a new batch).
        /// </summary>
        public void SendBackToMaker(string byUserId, string remarks)
        {
            if (Status != BatchStatus.UnderReview && Status != BatchStatus.UnderApproval)
                throw new InvalidOperationException("Can only send back a batch that's currently under review or approval.");
            Status = BatchStatus.Created;
            // "remarks" would be stored via the existing rejection-remarks mechanism (not modeled in this skeleton yet).
        }

        /// <summary> Final approver signs off. This Batch is now frozen FOREVER. </summary>
        public void Approve(string approverId)
        {
            if (Status != BatchStatus.UnderApproval)
                throw new InvalidOperationException("Only a batch under approval can be approved.");
            Status = BatchStatus.Approved;
        }
    }
}
