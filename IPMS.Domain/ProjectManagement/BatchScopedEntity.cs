namespace IPMS.Domain.Common
{
    /// <summary>
    /// Base class for every CONTENT entity that lives INSIDE a Batch
    /// (ProjectNumber, PostAssignment, Specialization, ExamPhaseDetail,
    /// TestStructureNode, and every remaining tab entity that follows the same
    /// pattern). Adds a BatchId that can be REASSIGNED — this is what makes
    /// "create a follow-up batch" work by repointing existing rows onto the new
    /// Batch, instead of cloning every row with a brand new Id. Because the
    /// row's own Id never changes, every OTHER row that references this row's Id
    /// (Exam Phase pointing at a PostAssignment, for example) stays correct
    /// automatically — nothing needs to be rewritten.
    ///
    /// Batch and Revision do NOT inherit from this. They ARE the fixed
    /// historical ledger — their own identity must never be repointed. Only
    /// inherit from this for entities that represent DATA living inside a
    /// batch, never for the batch/revision bookkeeping itself.
    /// </summary>
    public abstract class BatchScopedEntity<TId> : Entity<TId>
    {
        public long BatchId { get; private set; }

        // Used BOTH the first time a row is created (assigning it to whichever
        // Batch is currently active) AND later, when an Approved batch's
        // content is carried forward onto a brand-new follow-up Batch. Same
        // operation either way — "this row now belongs to Batch X" — so one
        // method covers both moments rather than having two separate ones.
        // Internal: only Project (and the entities that build their own
        // children, like ProjectNumber creating a PostAssignment) are allowed
        // to call this — nothing outside the Domain layer should ever move a
        // row between batches directly.
        internal void AssignBatch(long batchId) => BatchId = batchId;
    }
}