using Rnwood.Smtp4dev.Server.Imap;
using System.Linq;
using Xunit;

namespace Rnwood.Smtp4dev.Tests
{
    public class ImapFolderNamesTests
    {
        [Theory]
        [InlineData("inbox", "INBOX")]
        [InlineData("InBox", "INBOX")]
        [InlineData("DONE/", "DONE")]
        [InlineData("Archive/2024", "Archive/2024")]
        [InlineData("done", "done")]
        public void Normalize(string folderName, string expected)
        {
            Assert.Equal(expected, ImapFolderNames.Normalize(folderName));
        }

        [Fact]
        public void GetAncestors()
        {
            Assert.Equal(new[] { "a", "a/b" }, ImapFolderNames.GetAncestors("a/b/c").ToArray());
            Assert.Empty(ImapFolderNames.GetAncestors("a"));
        }

        [Theory]
        [InlineData("a/b", "a", true)]
        [InlineData("a/b/c", "a", true)]
        [InlineData("a", "a", false)]
        [InlineData("ab", "a", false)]
        public void IsDescendantOf(string folderName, string ancestor, bool expected)
        {
            Assert.Equal(expected, ImapFolderNames.IsDescendantOf(folderName, ancestor));
        }

        [Theory]
        [InlineData("DONE", "", "*", true)]
        [InlineData("DONE", "", "%", true)]
        [InlineData("DONE", "", "DONE", true)]
        [InlineData("DONE", "", "done", false)]
        [InlineData("INBOX", "", "inbox", true)]
        [InlineData("Archive/2024", "", "%", false)]
        [InlineData("Archive/2024", "", "*", true)]
        [InlineData("Archive/2024", "", "Archive/%", true)]
        [InlineData("Archive/2024", "Archive/", "%", true)]
        [InlineData("Archive", "Archive/", "%", false)]
        [InlineData("A.B", "", "A.B", true)]
        [InlineData("AxB", "", "A.B", false)]
        public void IsMatch(string folderName, string referenceName, string pattern, bool expected)
        {
            Assert.Equal(expected, ImapFolderNames.IsMatch(folderName, referenceName, pattern));
        }
    }
}
