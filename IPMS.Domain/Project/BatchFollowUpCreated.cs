namespace IPMS.Domain.Project
{
    /// <summary>
    /// Raised once when "Edit" is clicked on an Approved batch and a new Batch
    /// is spawned to carry its content forward. Deliberately ONE summary event
    /// per follow-up batch — NOT one event per row carried forward — so the
    /// audit log records something like "Batch 1005 created, carrying forward
    /// 200 rows" instead of 200 near-identical "BatchId changed" entries that
    /// would drown out the changes people actually care about.
    /// The Application layer's audit-log writer should turn this into exactly
    /// one audit row.
    /// </summary>
    public class BatchFollowUpCreated
    {
        public long PermanentProjectIdentificationNo { get; }
        public long PreviousBatchId { get; }
        public long NewBatchId { get; }
        public int RowsCarriedForward { get; }
        public string CreatedBy { get; }

        public BatchFollowUpCreated(long ppin, long previousBatchId, long newBatchId, int rowsCarriedForward, string createdBy)
        {
            PermanentProjectIdentificationNo = ppin;
            PreviousBatchId = previousBatchId;
            NewBatchId = newBatchId;
            RowsCarriedForward = rowsCarriedForward;
            CreatedBy = createdBy;
        }
    }
}