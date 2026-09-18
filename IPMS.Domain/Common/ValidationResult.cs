using System.Collections.Generic;
using System.Linq;

namespace IPMS.Domain.Common
{
    /// <summary>
    /// ERROR   = blocking. Stops Send-to-Review / Approve from happening at all.
    ///           Example: a Phase entry points at a Post/Specialization that no
    ///           longer exists (an "orphaned reference").
    /// WARNING = advisory only. NEVER blocks anything. The maker gets a prompt
    ///           ("are you sure you want to save this?") but can proceed anyway.
    ///           The reviewer/approver later see the same warning and decide
    ///           whether to move forward or send it back with remarks.
    ///           Example: a Penalty set at Subject level conflicts with the
    ///           Penalty already set at Phase level.
    /// </summary>
    public enum Severity { Error, Warning }

    /// <summary> One single problem found somewhere in the Project's data. </summary>
    public class ValidationMessage
    {
        public Severity Severity { get; }
        public string Message { get; }

        public ValidationMessage(Severity severity, string message)
        {
            Severity = severity;
            Message = message;
        }
    }

    /// <summary>
    /// Collects EVERY problem found across the whole Project in one pass, instead
    /// of stopping at the first one found — because someone filling in a 12-tab
    /// form wants to see everything wrong at once, not fix one thing, resubmit,
    /// and discover the next problem only then.
    /// </summary>
    public class ValidationResult
    {
        private readonly List<ValidationMessage> _messages = new();

        public void Add(Severity severity, string message)
            => _messages.Add(new ValidationMessage(severity, message));

        public void AddRange(IEnumerable<ValidationMessage> messages)
            => _messages.AddRange(messages);

        // Only ERRORS block a workflow transition. Warnings never do — see enum comment above.
        public bool HasBlockingErrors => _messages.Any(m => m.Severity == Severity.Error);

        public IReadOnlyList<ValidationMessage> Errors =>
            _messages.Where(m => m.Severity == Severity.Error).ToList();

        public IReadOnlyList<ValidationMessage> Warnings =>
            _messages.Where(m => m.Severity == Severity.Warning).ToList();
    }
}
