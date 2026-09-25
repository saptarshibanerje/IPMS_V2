using IPMS.Domain.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IPMS.Domain.ProjectManagement
{
    /// <summary>
    /// One row on the "Co-Ordinator(s)" tab — a contact person for the Project,
    /// tied to a specific Division. Plain contact fields, no pool/master-data
    /// dependency, so this is one of the simplest entities in the whole model.
    /// </summary>
    public class CoOrdinatorDetail : BatchScopedEntity<long>
    {
        public string DivisionName { get; private set; }
        public string Name { get; private set; }
        public string Email { get; private set; }
        public string PhoneNumber { get; private set; }

        private CoOrdinatorDetail() { }

        public static CoOrdinatorDetail Create(string divisionName, string name, string email, string phoneNumber)
            => new CoOrdinatorDetail { DivisionName = divisionName, Name = name, Email = email, PhoneNumber = phoneNumber };
    }
}
