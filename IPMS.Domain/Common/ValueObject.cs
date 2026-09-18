using System.Collections.Generic;
using System.Linq;

namespace IPMS.Domain.Common
{
    /// <summary>
    /// A ValueObject has NO identity of its own — it's just a bundle of values.
    /// Two ValueObjects with the same values ARE the same thing; you never ask
    /// "which one is this", only "what does it say".
    ///
    /// Example: Penalty = "One-Fourth (0.25)". There's no "Penalty #17" anywhere —
    /// if two rows both say "One-Fourth (0.25)", they're identical, interchangeable
    /// values, not two different things that happen to match.
    /// </summary>
    public abstract class ValueObject
    {
        // Every ValueObject lists which of its own fields make up its "value" —
        // used below to compare two ValueObjects by CONTENTS, not by reference.
        protected abstract IEnumerable<object> GetEqualityComponents();

        public override bool Equals(object obj)
        {
            if (obj is not ValueObject other || GetType() != other.GetType())
                return false;

            return GetEqualityComponents().SequenceEqual(other.GetEqualityComponents());
        }

        public override int GetHashCode()
        {
            return GetEqualityComponents()
                .Select(x => x?.GetHashCode() ?? 0)
                .Aggregate(0, (a, b) => a ^ b);
        }
    }
}
