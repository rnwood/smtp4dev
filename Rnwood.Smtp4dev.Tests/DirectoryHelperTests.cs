using System;
using System.IO;
using FluentAssertions;
using Xunit;

namespace Rnwood.Smtp4dev.Tests
{
    public class DirectoryHelperTests
    {
        private const string OverridePath = "C:\\temp\\";
        private readonly string appDataPath;

        public DirectoryHelperTests()
        {
            appDataPath = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "smtp4dev");
        }

        [Fact]
        public void WhenSpecifiedOnCommandLineOverrideDefault()
        {
            var dir = DirectoryHelper.GetDataDir(new CommandLineOptions()
            {
                BaseAppDataPath = OverridePath
            });

            dir.Should().Be(OverridePath);
        }

        [Fact]
        public void DefaultDataDirIsAppData()
        {
            
            var dir = DirectoryHelper.GetDataDir(new CommandLineOptions());
            dir.Should().Be(appDataPath);
        }
    }
}