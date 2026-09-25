using IPMS.Domain.Common;

namespace IPMS.Domain.ProjectManagement
{
    /// <summary>
    /// One "Post + Specialization" combination inside a Project (e.g.
    /// Post: "New General", Specialization: "test 2"). This has its OWN local Id,
    /// and every other tab (Exam Phases, Penalty, Sessions, Qualification,
    /// Candidate Count...) refers back to THIS Id from now on — never to the raw
    /// text — so if a Post/Specialization is deleted or renamed here, every other
    /// tab that points at it can be checked instead of silently breaking.
    /// </summary>
    public class PostSpecialization : Entity<long>
    {
        // Post / Specialization = a FROZEN copy of what was picked from the master
        // pool at the time (see PoolSnapshots.cs). Once picked, it belongs to this
        // Project forever — later edits to the master data never change this.
        public PostSnapshot Post { get; private set; }
        public PoolSnapshot Specialization { get; private set; }

        private PostSpecialization() { }

        public static PostSpecialization Create(PostSnapshot post, PoolSnapshot specialization)
            => new PostSpecialization { Post = post, Specialization = specialization };
    }
}
