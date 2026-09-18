using System;
using System.Collections.Generic;
using IPMS.Domain.Common;

namespace IPMS.Domain.Project.ExamStructure
{
    /// <summary>
    /// Checks fields that are now allowed to be set at MORE THAN ONE level of the
    /// tree (so far, just Penalty) for CONFLICTS — this is NOT a "completeness"
    /// check.
    ///
    /// The rule (confirmed with the business owner):
    ///   - A node's own value being empty is always fine, no matter what.
    ///   - If a node HAS a value, walk UP the tree to the nearest ANCESTOR that
    ///     also has a value for that same field (skipping ancestors that are empty).
    ///   - If that ancestor's value is DIFFERENT from this node's value -> flag it.
    ///   - This is NEVER a blocking error — always just a WARNING (see ValidationResult.cs).
    ///
    /// Adding a new field that becomes "allowed at multiple levels" later (e.g. if
    /// Mode or Duration ever needs this too) is ONE line added to the list below —
    /// no other code changes needed anywhere else in the system.
    /// </summary>
    public static class NodeConsistencyRules
    {
        private static readonly List<(string FieldName, Func<TestStructureNode, IComparable> Selector)> Rules = new()
        {
            ("Penalty", n => n.Penalty),
            // ("Mode", n => n.Mode),   <- example of how a future multi-level field gets added
        };

        public static IEnumerable<ValidationMessage> ValidateAll(TestStructureNode node)
        {
            foreach (var (fieldName, selector) in Rules)
            {
                var value = selector(node);
                if (value is null) continue;   // this node didn't set it — nothing to check

                var ancestor = node.Parent;
                while (ancestor is not null && selector(ancestor) is null)
                    ancestor = ancestor.Parent;   // climb until we find a set value, or run out of ancestors

                if (ancestor is not null && !value.Equals(selector(ancestor)))
                {
                    yield return new ValidationMessage(Severity.Warning,
                        $"{fieldName} at {node.Level} '{node.Name}' ({value}) doesn't match " +
                        $"{ancestor.Level} '{ancestor.Name}' ({selector(ancestor)}). " +
                        "This is allowed, but please confirm it's intentional.");
                }
            }
        }
    }
}
