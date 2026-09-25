using IPMS.Domain.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IPMS.Domain.ProjectManagement
{
    /// <summary>
    /// The single free-text note on the "Note/Additional Info" tab. Unlike
    /// every other entity in this file group, there's only ever ONE of these
    /// per Batch, not a list — Project keeps a single reference to it (see
    /// Project.Note) rather than a collection.
    /// </summary>
    public class ExplanationNote : BatchScopedEntity<long>
    {
        public string Text { get; private set; }

        private ExplanationNote() { }

        public static ExplanationNote Create(string text) => new ExplanationNote { Text = text };

        public void UpdateText(string text) => Text = text;
    }
}
