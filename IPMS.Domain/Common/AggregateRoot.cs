using System.Collections.Generic;

namespace IPMS.Domain.Common
{
    /// <summary>
    /// Marks an Entity as an AGGREGATE ROOT — the ONE "front door" Entity that the
    /// rest of the application is allowed to load and save directly (through a
    /// Repository). Everything INSIDE the aggregate (its child Entities/
    /// ValueObjects) can only be reached THROUGH the root — nobody is allowed to
    /// load a child directly and save it on its own. This is what keeps the whole
    /// group of related data consistent as one unit.
    ///
    /// In our system: Project is the ONLY AggregateRoot. Batch, Revision,
    /// PostSpecialization, TestStructureNode etc. are all Entities that live
    /// INSIDE Project — never loaded or saved independently of it.
    ///
    /// This base class also collects "Domain Events" — a record of "something
    /// meaningful happened" (e.g. ProjectApproved) that other parts of the system
    /// (audit logging, email notifications, snapshot creation) can react to later,
    /// without Project needing to know anything about emails or audit tables.
    /// </summary>
    public abstract class AggregateRoot<TId> : Entity<TId>
    {
        private readonly List<object> _domainEvents = new();
        public IReadOnlyCollection<object> DomainEvents => _domainEvents.AsReadOnly();

        protected void RaiseEvent(object domainEvent) => _domainEvents.Add(domainEvent);

        // Called by the Application layer after it has finished processing/saving
        // the events (e.g. writing audit rows), so the same events aren't raised twice.
        public void ClearEvents() => _domainEvents.Clear();
    }
}
