using System.Collections.Generic;
using IPMS.Domain.Common;

namespace IPMS.Domain.Project
{
    /// <summary>
    /// A "pool" field is anything typed/selected from shared master data (Post,
    /// Specialization, Client Org, Additional Services, etc.) with autocomplete
    /// or a dropdown. Once the user picks it (or types something brand new), we
    /// take a FROZEN COPY of it into the Project — we never look the master
    /// record up again after that moment.
    /// Your rule: "once it's in the project, it's the project's own scope, forever."
    ///
    /// MasterId is kept ONLY as a breadcrumb, so you can trace where a value
    /// originally came from — it is NEVER used to re-fetch fresh data later.
    /// </summary>
    public class PoolSnapshot : ValueObject
    {
        public long? MasterId { get; private set; }
        public string Text { get; private set; }

        private PoolSnapshot() { }

        public static PoolSnapshot Of(long? masterId, string text)
            => new PoolSnapshot { MasterId = masterId, Text = text };

        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return MasterId;
            yield return Text;
        }
    }

    /// <summary>
    /// Same idea as PoolSnapshot, but for Client Organization, which carries
    /// EXTRA fields (abbreviation, type of org) that get used elsewhere — e.g.
    /// OrgAbbr feeds directly into the generated Project Number.
    /// </summary>
    public class ClientOrgSnapshot : ValueObject
    {
        public long? MasterId { get; private set; }
        public string OrgName { get; private set; }
        public string OrgAbbr { get; private set; }
        public string TypeOfOrg { get; private set; }

        private ClientOrgSnapshot() { }

        public static ClientOrgSnapshot Of(long? masterId, string orgName, string orgAbbr, string typeOfOrg)
            => new ClientOrgSnapshot { MasterId = masterId, OrgName = orgName, OrgAbbr = orgAbbr, TypeOfOrg = typeOfOrg };

        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return MasterId;
            yield return OrgName;
            yield return OrgAbbr;
            yield return TypeOfOrg;
        }
    }
    /// <summary>
    /// Same idea again, for a SubOrganization — the optional "parent" a Post can
    /// sit under (e.g. an Organization can have multiple SubOrgs; a Post may or
    /// may not belong to one). Frozen the same way as every other pool field —
    /// once picked, it belongs to this specific PostAssignment forever.
    /// </summary>
    public class SubOrganizationSnapshot : ValueObject
    {
        public long? MasterId { get; private set; }
        public string Text { get; private set; }

        private SubOrganizationSnapshot() { }

        public static SubOrganizationSnapshot Of(long? masterId, string text)
            => new SubOrganizationSnapshot { MasterId = masterId, Text = text };

        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return MasterId;
            yield return Text;
        }
    }

    /// <summary> Same idea again, for Post — which also carries extra fields. </summary>
    public class PostSnapshot : ValueObject
    {
        public long? MasterId { get; private set; }
        public string PostName { get; private set; }
        public string PostAbbr { get; private set; }
        public string ProjectType { get; private set; }
        public bool IsRegistrationRequired { get; private set; }

        private PostSnapshot() { }

        public static PostSnapshot Of(long? masterId, string postName, string postAbbr, string projectType, bool isRegistrationRequired)
            => new PostSnapshot { MasterId = masterId, PostName = postName, PostAbbr = postAbbr, ProjectType = projectType, IsRegistrationRequired = isRegistrationRequired };

        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return MasterId;
            yield return PostName;
            yield return PostAbbr;
            yield return ProjectType;
            yield return IsRegistrationRequired;
        }
    }
}
