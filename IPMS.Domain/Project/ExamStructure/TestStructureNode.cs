using System.Collections.Generic;
using IPMS.Domain.Common;

namespace IPMS.Domain.Project.ExamStructure
{
    /// <summary>
    /// ONE node in the exam structure tree. This single class represents a
    /// Subject, a Section under it, OR a SubSection under that — same class,
    /// same table, at every depth. This replaces the old design of having a
    /// separate table for "Objective Section" and a separate table for
    /// "Descriptive Section" (which broke the moment a new test type like
    /// "Interview" showed up and didn't fit either shape).
    ///
    /// NOT every field applies to every node — e.g. an Interview node will have
    /// MaxMarks filled in but NumQuestions left empty, and that's completely
    /// valid (see the Project's overall rule: "if a field exists it must be
    /// correct; if it's missing, that's fine").
    ///
    /// NodeTypeId points at master data (Objective / Descriptive / Interview /
    /// Group Discussion / anything invented later) — adding a brand-new type
    /// never needs a code change, just a new master-data row.
    /// </summary>
    public class TestStructureNode : BatchScopedEntity<long>
    {
        public long? ParentNodeId { get; private set; }   // null = this is a top-level Subject
        public NodeLevel Level { get; private set; }
        public long NodeTypeId { get; private set; }        // -> master data: Objective, Descriptive, Interview...
        public string Name { get; private set; }
        public int SequenceNo { get; private set; }

        // The "generous, mostly-nullable" field set — every field any known test
        // type could need. A node simply leaves the ones it doesn't need empty.
        public decimal? MaxMarks { get; private set; }
        public int? NumQuestions { get; private set; }
        public decimal? Penalty { get; private set; }          // can live at ANY level now — see NodeConsistencyRules.cs
        public int? AttemptCount { get; private set; }           // "To Be Attempt" — recently made valid at any level too
        public string Mode { get; private set; }                  // Online / Offline
        public string Duration { get; private set; }
        public bool? IsQualifying { get; private set; }
        public bool? IsVisible { get; private set; }

        public List<TestStructureNode> Children { get; private set; } = new();

        // NOT stored in the DB directly — set in memory when the whole tree is
        // loaded, so a node can look "upward" at its own ancestors (used by the
        // cross-level Penalty check).
        public TestStructureNode Parent { get; private set; }

        private TestStructureNode() { }

        public static TestStructureNode CreateSubject(string name, long nodeTypeId, int sequenceNo)
            => new TestStructureNode { Level = NodeLevel.Subject, Name = name, NodeTypeId = nodeTypeId, SequenceNo = sequenceNo };

        public TestStructureNode AddChild(string name, long nodeTypeId, NodeLevel level, int sequenceNo)
        {
            var child = new TestStructureNode
            {
                ParentNodeId = Id,
                Parent = this,
                Level = level,
                Name = name,
                NodeTypeId = nodeTypeId,
                SequenceNo = sequenceNo
            };
            child.AssignBatch(BatchId);   // starts life in the same batch as its parent node
            Children.Add(child);
            return child;
        }

        /// <summary> Walks this node plus every descendant — used when validating the whole tree at once. </summary>
        public IEnumerable<TestStructureNode> SelfAndDescendants()
        {
            yield return this;
            foreach (var child in Children)
                foreach (var descendant in child.SelfAndDescendants())
                    yield return descendant;
        }
    }
}
