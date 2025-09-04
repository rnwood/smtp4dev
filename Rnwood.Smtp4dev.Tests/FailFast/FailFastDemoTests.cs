using System;
using System.IO;
using Xunit;

namespace Rnwood.Smtp4dev.Tests.FailFast
{
    /// <summary>
    /// Temporary test to demonstrate fail-fast behavior in CI.
    /// This test can be enabled to show how fail-fast stops the build matrix quickly.
    /// </summary>
    public class FailFastDemoTests
    {
        [Fact(Skip = "Enable this test to demonstrate fail-fast behavior - will cause immediate build failure")]
        public void DemonstrateFail_Fast_Behavior()
        {
            // This test is intentionally designed to fail to demonstrate fail-fast
            // When enabled (by removing Skip), it will cause the BuildMatrix to fail fast
            // and cancel remaining platform builds, showing the fail-fast functionality
            
            Assert.Fail("This test intentionally fails to demonstrate fail-fast CI behavior. " +
                       "When this test runs, it should cause the BuildMatrix to stop other platform builds immediately, " +
                       "rather than waiting for all platforms to complete.");
        }
        
        [Fact]
        public void FailFast_Documentation_Exists()
        {
            // This test verifies that the fail-fast documentation was created
            var docPath = Path.Combine(GetRepositoryRoot(), "docs", "ci-fail-fast.md");
            Assert.True(File.Exists(docPath), $"Fail-fast documentation should exist at: {docPath}");
            
            var content = File.ReadAllText(docPath);
            Assert.Contains("CI Fail-Fast Configuration", content);
            Assert.Contains("failFast: true", content);
        }
        
        private static string GetRepositoryRoot()
        {
            var currentDir = Directory.GetCurrentDirectory();
            while (currentDir != null && !File.Exists(Path.Combine(currentDir, "azure-pipelines.yml")))
            {
                currentDir = Directory.GetParent(currentDir)?.FullName;
            }
            return currentDir ?? throw new InvalidOperationException("Could not find repository root");
        }
    }
}