namespace IPMS.Domain.Project
{
    /// <summary>
    /// The states a Batch moves through. See the lifecycle diagram we discussed:
    /// Created -> UnderReview -> UnderApproval -> Approved, with "Send back to
    /// maker" un-freezing back to Created from either Review or Approval.
    /// </summary>
    public enum BatchStatus
    {
        Created=1,        // Editable draft. Maker can freely change anything.
        UnderReview=2,    // FROZEN. Nobody can edit. Waiting for the reviewer to look at it.
        UnderApproval=3,  // FROZEN. Reviewer approved and forwarded it. Waiting for final approver.
        Approved=4        // FROZEN, PERMANENTLY. This Batch is now history — editing
                        // again always creates a brand NEW Batch, never reopens this one.
    }
}
