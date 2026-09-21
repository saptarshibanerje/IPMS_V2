using IPMS.Domain.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IPMS.Domain.Project
{
    /// <summary>
    /// One Post "declared" under a ProjectNumber — e.g. "Post B, under SubOrg A".
    /// This is the REAL reusable unit with its own Id — not Post text by itself.
    ///
    /// Important real-world rule this class enforces just by its shape: the SAME
    /// Post can legitimately be declared more than once with different parents —
    /// e.g. "SubOrg A + Post B" and "SubOrg B + Post B" are two separate
    /// PostAssignment rows, each with their own Id, even though both mention
    /// "Post B". They are NOT the same thing just because the Post matches.
    ///
    /// Every PostAssignment belongs to exactly ONE ProjectNumber — it is created
    /// through ProjectNumber.AddPostAssignment(...) and never shared or reused
    /// across a different ProjectNumber, even if the content would be identical.
    ///
    /// SubOrganization is OPTIONAL (a Post can stand alone, with no parent
    /// SubOrg). Specializations are also OPTIONAL — a Post can have zero, one,
    /// or many — but a Specialization can NEVER exist without its parent
    /// PostAssignment (there is no way to create one except through this class).
    /// </summary>
    public class PostAssignment : BatchScopedEntity<long>
    {
        public SubOrganizationSnapshot SubOrganization { get; private set; }   // nullable — parent, if any
        public PostSnapshot Post { get; private set; }                          // required

        public List<Specialization> Specializations { get; private set; } = new();

        private PostAssignment() { }

        public static PostAssignment Create(PostSnapshot post, SubOrganizationSnapshot subOrganization = null)
            => new PostAssignment { Post = post, SubOrganization = subOrganization };

        /// <summary>
        /// The ONLY way a Specialization can come into existence — always
        /// through its parent PostAssignment, never on its own. This is what
        /// makes "a Specialization without a Post" structurally impossible,
        /// rather than just a validation rule someone could forget to check.
        /// A brand-new Specialization starts life in the SAME Batch as its
        /// parent PostAssignment — no separate assignment needed by the caller.
        /// </summary>
        public Specialization AddSpecialization(PoolSnapshot specializationValue)
        {
            var specialization = Specialization.Create(specializationValue);
            specialization.AssignBatch(BatchId);
            Specializations.Add(specialization);
            return specialization;
        }
    }

    /// <summary>
    /// A Specialization under a Post (e.g. "Agriculture Officer" under
    /// "Scientific Officer"). Has its own Id — every downstream tab (Exam
    /// Phases, Penalty, Sessions...) can reference this Id directly, same as it
    /// references PostAssignment.Id — but a Specialization can only ever be
    /// created via PostAssignment.AddSpecialization(...) above.
    /// </summary>
    public class Specialization : BatchScopedEntity<long>
    {
        public PoolSnapshot Value { get; private set; }

        private Specialization() { }

        internal static Specialization Create(PoolSnapshot value) => new Specialization { Value = value };
    }
}
