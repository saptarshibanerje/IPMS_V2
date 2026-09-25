using System;
using System.Collections.Generic;
using IPMS.Domain.Common;

namespace IPMS.Domain.ProjectManagement
{
    /// <summary>
    /// The fields from the "Initial Information" tab that can be changed at
    /// ANY time (unlike Client Category/Organization/Project Type/Project
    /// Title/Short Description, which live directly on Project and get
    /// permanently locked once the first batch is ever approved — see
    /// Project.SetFoundationalDetails()).
    ///
    /// Only ONE of these per batch, not a list — same pattern as
    /// ExplanationNote.cs. Project keeps a single reference to it and either
    /// creates it the first time or updates it in place on later saves.
    /// </summary>
    public class ProjectInitialDetails : BatchScopedEntity<long>
    {
        public string ContactPersonName { get; private set; }
        public string ContactPersonEmail { get; private set; }
        public string ContactNumber { get; private set; }

        public PoolSnapshot RollNumberStructure { get; private set; }          // e.g. "10-Digit"
        public List<PoolSnapshot> AdditionalServices { get; private set; } = new();   // e.g. CCTV, HHMD
        public List<PoolSnapshot> PreExamActivityHandledBy { get; private set; } = new();  // master pool, confirmed

        public string DispatchMode { get; private set; }                        // e.g. "NA"
        public bool HandwritingSampleRequired { get; private set; }
        public bool IsMultipostAllowed { get; private set; }
        public int NumberOfProjectNumbers { get; private set; }
        public List<DateTime> ResultToBeSharedOn { get; private set; } = new();

        private ProjectInitialDetails() { }

        public static ProjectInitialDetails Create() => new ProjectInitialDetails();

        /// <summary>
        /// One combined update, matching the tab's single "Save &amp; Next"
        /// action — the whole tab is saved together, so the whole entity is
        /// updated together, rather than one setter per field.
        /// </summary>
        public void Update(
            string contactPersonName, string contactPersonEmail, string contactNumber,
            PoolSnapshot rollNumberStructure, List<PoolSnapshot> additionalServices,
            List<PoolSnapshot> preExamActivityHandledBy, string dispatchMode,
            bool handwritingSampleRequired, bool isMultipostAllowed,
            int numberOfProjectNumbers, List<DateTime> resultToBeSharedOn)
        {
            ContactPersonName = contactPersonName;
            ContactPersonEmail = contactPersonEmail;
            ContactNumber = contactNumber;
            RollNumberStructure = rollNumberStructure;
            AdditionalServices = additionalServices ?? new List<PoolSnapshot>();
            PreExamActivityHandledBy = preExamActivityHandledBy ?? new List<PoolSnapshot>();
            DispatchMode = dispatchMode;
            HandwritingSampleRequired = handwritingSampleRequired;
            IsMultipostAllowed = isMultipostAllowed;
            NumberOfProjectNumbers = numberOfProjectNumbers;
            ResultToBeSharedOn = resultToBeSharedOn ?? new List<DateTime>();
        }
    }
}