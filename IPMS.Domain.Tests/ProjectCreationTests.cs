//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//namespace IPMS.Domain.Tests
//{
//    class ProjectCreationTests
//    {
//    }
//}

using Xunit;
using IPMS.Domain.ProjectManagement;

namespace IPMS.Domain.Tests
{
    // A test class is just a normal C# class. xUnit finds every method
    // inside it marked with [Fact] and runs each one independently.
    public class ProjectCreationTests
    {
        // The method NAME is the test's documentation — when this fails,
        // the test runner shows you this exact name, so make it describe
        // the behaviour being checked, not just "Test1".
        [Fact]
        public void CreateNew_StartsWithExactlyOneBatch_InCreatedStatus()
        {
            // ----- Arrange -----
            // Set up the inputs the method under test needs.
            long permanentProjectId = 0;
            string divisionOwner = "CR I";
            string createdBy = "Creator";

            // ----- Act -----
            // Call the ONE thing this test is actually about.
            var project = Project.CreateNew(divisionOwner, createdBy);

            // ----- Assert -----
            // Check the outcome. If either of these is false, the test
            // FAILS and xUnit prints exactly what it expected vs what it got.
            Assert.Single(project.Batches);                          // "there should be exactly 1 item in this list"
            Assert.Equal(BatchStatus.Created, project.LatestBatch.Status);
            
        }
    }
}