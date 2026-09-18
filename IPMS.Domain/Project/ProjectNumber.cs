using IPMS.Domain.Common;

namespace IPMS.Domain.Project
{
    /// <summary>
    /// The running-serial Project Number, e.g. "IBPS/SEL/0001".
    /// OrgAbbr/ProjectTypeAbbr START OUT copied from the ClientOrgSnapshot/PostSnapshot
    /// when this is first created, but AFTER that they live entirely inside the
    /// Project and can be freely edited at any time — editing them here never
    /// touches master data, and later master data edits never touch this.
    /// </summary>
    public class ProjectNumber : Entity<long>
    {
        public string OrgAbbr { get; private set; }
        public string ProjectTypeAbbr { get; private set; }
        public string RunningSerial { get; private set; }
        public bool IsConfirmed { get; private set; }

        public string FullNumber => $"{OrgAbbr}/{ProjectTypeAbbr}/{RunningSerial}";

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
    }
}
