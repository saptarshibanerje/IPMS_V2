namespace IPMS.Domain.ProjectManagement.ExamStructure
{
    /// <summary>
    /// How deep in the tree a node sits. A "Subject" is the top level (e.g.
    /// "Test 1"). A "Section" is one level down (e.g. its Objective part, its
    /// Descriptive part — or for an Interview/Group-Discussion-type subject,
    /// there might just be one Section with only Marks filled in). "SubSection"
    /// is one level further down — this is what used to be called a
    /// "weightage row".
    /// </summary>
    public enum NodeLevel { Subject, Section, SubSection }
}
