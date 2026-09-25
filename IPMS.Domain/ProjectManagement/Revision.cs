using System;
using System.Collections.Generic;
using System.Linq;
using IPMS.Domain.Common;

namespace IPMS.Domain.ProjectManagement
{
    /// <summary>
    /// A REVISION is NOT a copy of data — it's just a "bookmark" pointing at one
    /// specific Approved Batch, marking the moment the Project was published /
    /// intimated to OTHER divisions. Other divisions only ever see Revisions —
    /// never individual Batches, and never know which Batch a Revision came from.
    ///
    /// Maps to your existing tbl_project_intimation_details table — this class
    /// just gives it real behaviour instead of being a plain data row.
    /// </summary>
    public class Revision : Entity<long>
    {
        public long ProjectIdentificationNo { get; private set; }
        public long SourceBatchId { get; private set; }   // the Approved batch this bookmark points to
        public int VersionNumber { get; private set; }      // R0, R1, R2...

        private Revision() { }

        /// <summary>
        /// Cuts a new Revision from whichever Approved batch is CURRENTLY the
        /// latest one not already claimed by an earlier Revision.
        /// "existingRevisions" is passed in so we can enforce: two Revisions can
        /// never point at the same Batch, and version numbers always increase.
        /// </summary>
        public static Revision CreateFrom(Batch approvedBatch, IEnumerable<Revision> existingRevisions, string createdBy)
        {
            if (approvedBatch.Status != BatchStatus.Approved)
                throw new InvalidOperationException("A revision can only be cut from an Approved batch.");

            var revisionsList = existingRevisions.ToList();

            if (revisionsList.Any(r => r.SourceBatchId == approvedBatch.Id))
                throw new InvalidOperationException("This batch has already been published as a revision.");

            var nextVersionNumber = revisionsList.Any()
                ? revisionsList.Max(r => r.VersionNumber) + 1
                : 0;

            return new Revision
            {
                ProjectIdentificationNo = approvedBatch.ProjectIdentificationNo,
                SourceBatchId = approvedBatch.Id,
                VersionNumber = nextVersionNumber,
                CreatedBy = createdBy,
                CreatedDate = DateTime.UtcNow
            };
        }
    }
}
