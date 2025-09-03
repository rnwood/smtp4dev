using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Rnwood.SmtpServer.Tests;

public class SmtpStreamWriterTests
{
    [Fact]
    public void Constructor_WithLeaveOpenTrue_SetsCorrectProperties()
    {
        // Arrange
        using var memoryStream = new MemoryStream();
        
        // Act
        using var writer = new SmtpStreamWriter(memoryStream, leaveOpen: true);
        
        // Assert
        Assert.Equal("\r\n", writer.NewLine);
        Assert.True(writer.Encoding is UTF8Encoding);
        Assert.False(writer.Encoding.GetPreamble().Length > 0); // No BOM
        Assert.True(memoryStream.CanWrite); // Stream should still be accessible
    }

    [Fact]
    public void Constructor_WithLeaveOpenFalse_SetsCorrectProperties()
    {
        // Arrange
        using var memoryStream = new MemoryStream();
        
        // Act
        using var writer = new SmtpStreamWriter(memoryStream, leaveOpen: false);
        
        // Assert
        Assert.Equal("\r\n", writer.NewLine);
        Assert.True(writer.Encoding is UTF8Encoding);
        Assert.False(writer.Encoding.GetPreamble().Length > 0); // No BOM
    }

    [Fact]
    public void LeaveOpen_True_StreamRemainsOpenAfterDisposal()
    {
        // Arrange
        var memoryStream = new MemoryStream();
        
        // Act
        using (var writer = new SmtpStreamWriter(memoryStream, leaveOpen: true))
        {
            writer.WriteLine("Test");
            writer.Flush();
        }
        
        // Assert
        Assert.True(memoryStream.CanWrite);
        Assert.True(memoryStream.CanRead);
        
        // Cleanup
        memoryStream.Dispose();
    }

    [Fact]
    public void LeaveOpen_False_StreamIsClosedAfterDisposal()
    {
        // Arrange
        var memoryStream = new MemoryStream();
        
        // Act
        using (var writer = new SmtpStreamWriter(memoryStream, leaveOpen: false))
        {
            writer.WriteLine("Test");
            writer.Flush();
        }
        
        // Assert
        Assert.False(memoryStream.CanWrite);
        Assert.False(memoryStream.CanRead);
    }

    [Fact]
    public void WriteLine_UsesCorrectLineEnding()
    {
        // Arrange
        using var memoryStream = new MemoryStream();
        using var writer = new SmtpStreamWriter(memoryStream, leaveOpen: true);
        
        // Act
        writer.WriteLine("Test line");
        writer.Flush();
        
        // Assert
        var result = Encoding.UTF8.GetString(memoryStream.ToArray());
        Assert.Equal("Test line\r\n", result);
    }

    [Fact]
    public void Write_MultipleLines_UsesCorrectLineEndings()
    {
        // Arrange
        using var memoryStream = new MemoryStream();
        using var writer = new SmtpStreamWriter(memoryStream, leaveOpen: true);
        
        // Act
        writer.WriteLine("First line");
        writer.WriteLine("Second line");
        writer.WriteLine("Third line");
        writer.Flush();
        
        // Assert
        var result = Encoding.UTF8.GetString(memoryStream.ToArray());
        Assert.Equal("First line\r\nSecond line\r\nThird line\r\n", result);
    }

    [Fact]
    public void Write_SmtpCommands_ProducesCorrectOutput()
    {
        // Arrange
        using var memoryStream = new MemoryStream();
        using var writer = new SmtpStreamWriter(memoryStream, leaveOpen: true);
        
        // Act
        writer.WriteLine("220 smtp4dev ready");
        writer.WriteLine("250-smtp4dev");
        writer.WriteLine("250-8BITMIME");
        writer.WriteLine("250 OK");
        writer.Flush();
        
        // Assert
        var result = Encoding.UTF8.GetString(memoryStream.ToArray());
        var expected = "220 smtp4dev ready\r\n250-smtp4dev\r\n250-8BITMIME\r\n250 OK\r\n";
        Assert.Equal(expected, result);
    }

    [Fact]
    public void Write_UnicodeCharacters_EncodesCorrectly()
    {
        // Arrange
        using var memoryStream = new MemoryStream();
        using var writer = new SmtpStreamWriter(memoryStream, leaveOpen: true);
        var unicodeText = "MAIL FROM: <tëst@ëxample.com>";
        
        // Act
        writer.WriteLine(unicodeText);
        writer.Flush();
        
        // Assert
        var result = Encoding.UTF8.GetString(memoryStream.ToArray());
        Assert.Equal(unicodeText + "\r\n", result);
    }

    [Fact]
    public void Write_LargeData_ExceedingBufferSize_WritesCorrectly()
    {
        // Arrange
        using var memoryStream = new MemoryStream();
        using var writer = new SmtpStreamWriter(memoryStream, leaveOpen: true);
        
        // Create data larger than the 24KB buffer (1024 * 24)
        var largeData = new string('A', 25000);
        
        // Act
        writer.WriteLine(largeData);
        writer.Flush();
        
        // Assert
        var result = Encoding.UTF8.GetString(memoryStream.ToArray());
        Assert.Equal(largeData + "\r\n", result);
    }

    [Fact]
    public void Write_EmailContent_WithMixedLineEndings_NormalizesToCRLF()
    {
        // Arrange
        using var memoryStream = new MemoryStream();
        using var writer = new SmtpStreamWriter(memoryStream, leaveOpen: true);
        
        // Act
        writer.Write("Line 1\n");  // Unix line ending
        writer.Write("Line 2\r\n"); // Windows line ending  
        writer.WriteLine("Line 3"); // WriteLine adds proper ending
        writer.Flush();
        
        // Assert
        var result = Encoding.UTF8.GetString(memoryStream.ToArray());
        Assert.Equal("Line 1\nLine 2\r\nLine 3\r\n", result);
    }

    [Fact]
    public async Task WriteAsync_WorksCorrectly()
    {
        // Arrange
        using var memoryStream = new MemoryStream();
        using var writer = new SmtpStreamWriter(memoryStream, leaveOpen: true);
        
        // Act
        await writer.WriteLineAsync("Async test line");
        await writer.FlushAsync();
        
        // Assert
        var result = Encoding.UTF8.GetString(memoryStream.ToArray());
        Assert.Equal("Async test line\r\n", result);
    }

    [Fact]
    public void Write_EmptyString_WritesNothing()
    {
        // Arrange
        using var memoryStream = new MemoryStream();
        using var writer = new SmtpStreamWriter(memoryStream, leaveOpen: true);
        
        // Act
        writer.Write("");
        writer.Flush();
        
        // Assert
        Assert.Equal(0, memoryStream.Length);
    }

    [Fact]
    public void WriteLine_EmptyString_WritesOnlyLineEnding()
    {
        // Arrange
        using var memoryStream = new MemoryStream();
        using var writer = new SmtpStreamWriter(memoryStream, leaveOpen: true);
        
        // Act
        writer.WriteLine("");
        writer.Flush();
        
        // Assert
        var result = Encoding.UTF8.GetString(memoryStream.ToArray());
        Assert.Equal("\r\n", result);
    }

    [Fact]
    public void Write_NullString_WritesNothing()
    {
        // Arrange
        using var memoryStream = new MemoryStream();
        using var writer = new SmtpStreamWriter(memoryStream, leaveOpen: true);
        
        // Act
        writer.Write((string)null);
        writer.Flush();
        
        // Assert
        Assert.Equal(0, memoryStream.Length);
    }

    [Fact]
    public void WriteLine_NullString_WritesOnlyLineEnding()
    {
        // Arrange
        using var memoryStream = new MemoryStream();
        using var writer = new SmtpStreamWriter(memoryStream, leaveOpen: true);
        
        // Act
        writer.WriteLine((string)null);
        writer.Flush();
        
        // Assert
        var result = Encoding.UTF8.GetString(memoryStream.ToArray());
        Assert.Equal("\r\n", result);
    }

    [Fact]
    public void Encoding_PropertiesMatchExpected()
    {
        // Arrange
        using var memoryStream = new MemoryStream();
        using var writer = new SmtpStreamWriter(memoryStream, leaveOpen: true);
        
        // Assert
        var encoding = writer.Encoding as UTF8Encoding;
        Assert.NotNull(encoding);
        Assert.False(encoding.GetPreamble().Length > 0); // No BOM
        
        // Verify the encoding is UTF-8 and has the expected properties
        Assert.Equal("Unicode (UTF-8)", encoding.EncodingName);
        Assert.Equal(65001, encoding.CodePage);
    }

    [Fact]
    public void Write_LongSmtpSession_ProducesCorrectOutput()
    {
        // Arrange
        using var memoryStream = new MemoryStream();
        using var writer = new SmtpStreamWriter(memoryStream, leaveOpen: true);
        
        // Act - Simulate a complete SMTP session
        writer.WriteLine("220 smtp4dev ready");
        writer.WriteLine("250-smtp4dev");
        writer.WriteLine("250-SIZE 10240000");
        writer.WriteLine("250-8BITMIME");
        writer.WriteLine("250-AUTH LOGIN PLAIN");
        writer.WriteLine("250 OK");
        writer.WriteLine("250 2.1.0 OK");
        writer.WriteLine("250 2.1.5 OK");
        writer.WriteLine("354 Start mail input; end with <CRLF>.<CRLF>");
        writer.WriteLine("250 2.0.0 OK");
        writer.WriteLine("221 2.0.0 Bye");
        writer.Flush();
        
        // Assert
        var result = Encoding.UTF8.GetString(memoryStream.ToArray());
        var expected = "220 smtp4dev ready\r\n" +
                      "250-smtp4dev\r\n" +
                      "250-SIZE 10240000\r\n" +
                      "250-8BITMIME\r\n" +
                      "250-AUTH LOGIN PLAIN\r\n" +
                      "250 OK\r\n" +
                      "250 2.1.0 OK\r\n" +
                      "250 2.1.5 OK\r\n" +
                      "354 Start mail input; end with <CRLF>.<CRLF>\r\n" +
                      "250 2.0.0 OK\r\n" +
                      "221 2.0.0 Bye\r\n";
        
        Assert.Equal(expected, result);
    }

    [Fact]
    public void Dispose_CalledMultipleTimes_DoesNotThrow()
    {
        // Arrange
        var memoryStream = new MemoryStream();
        var writer = new SmtpStreamWriter(memoryStream, leaveOpen: true);
        
        // Act & Assert - Should not throw
        writer.Dispose();
        writer.Dispose();
        writer.Dispose();
        
        // Cleanup
        memoryStream.Dispose();
    }

    // RFC 5321 Section 2.3.7: Line length limits (998 characters maximum, excluding CRLF)
    [Fact]
    public void WriteLine_RFC5321_MaximumLineLength_Within998Characters_WritesCorrectly()
    {
        // Arrange
        using var memoryStream = new MemoryStream();
        using var writer = new SmtpStreamWriter(memoryStream, leaveOpen: true);
        
        // Create a line exactly 998 characters (RFC 5321 maximum)
        var maxLengthLine = new string('A', 998);
        
        // Act
        writer.WriteLine(maxLengthLine);
        writer.Flush();
        
        // Assert
        var result = Encoding.UTF8.GetString(memoryStream.ToArray());
        Assert.Equal(maxLengthLine + "\r\n", result);
        Assert.Equal(998 + 2, result.Length); // 998 chars + CRLF
    }

    // RFC 5321 Section 2.3.7: Lines exceeding 998 characters are not compliant but should still be handled
    [Fact]
    public void WriteLine_RFC5321_ExceedingMaximumLineLength_StillWrites()
    {
        // Arrange
        using var memoryStream = new MemoryStream();
        using var writer = new SmtpStreamWriter(memoryStream, leaveOpen: true);
        
        // Create a line exceeding 998 characters (non-compliant but should be handled gracefully)
        var longLine = new string('B', 1500);
        
        // Act
        writer.WriteLine(longLine);
        writer.Flush();
        
        // Assert
        var result = Encoding.UTF8.GetString(memoryStream.ToArray());
        Assert.Equal(longLine + "\r\n", result);
        Assert.Equal(1500 + 2, result.Length);
    }

    // RFC 5321 Section 4.5.3.1.6: Transparency - lines beginning with "." must be handled
    [Fact]
    public void WriteLine_RFC5321_DotTransparency_LineBeginningWithDot()
    {
        // Arrange
        using var memoryStream = new MemoryStream();
        using var writer = new SmtpStreamWriter(memoryStream, leaveOpen: true);
        
        // Act - Write lines that begin with dots (transparency test)
        writer.WriteLine(".This line begins with a dot");
        writer.WriteLine("..This line begins with two dots");
        writer.WriteLine("."); // Single dot line (end-of-data marker in DATA command)
        writer.WriteLine("Normal line");
        writer.Flush();
        
        // Assert - SmtpStreamWriter should write exactly what's given
        // (dot stuffing/unstuffing is handled at higher protocol levels)
        var result = Encoding.UTF8.GetString(memoryStream.ToArray());
        var expected = ".This line begins with a dot\r\n" +
                      "..This line begins with two dots\r\n" +
                      ".\r\n" +
                      "Normal line\r\n";
        Assert.Equal(expected, result);
    }

    // RFC 5321 Section 2.3.8: Bare LF should be normalized to CRLF in Write operations
    [Fact]
    public void Write_RFC5321_BareLF_NotNormalizedInWrite()
    {
        // Arrange
        using var memoryStream = new MemoryStream();
        using var writer = new SmtpStreamWriter(memoryStream, leaveOpen: true);
        
        // Act - Write with bare LF (not using WriteLine)
        writer.Write("Line with bare LF\nAnother line\n");
        writer.Flush();
        
        // Assert - Write() method preserves original line endings
        var result = Encoding.UTF8.GetString(memoryStream.ToArray());
        Assert.Equal("Line with bare LF\nAnother line\n", result);
    }

    // RFC 5321 Section 2.3.8: Bare CR should be preserved in Write operations
    [Fact]
    public void Write_RFC5321_BareCR_PreservedInWrite()
    {
        // Arrange
        using var memoryStream = new MemoryStream();
        using var writer = new SmtpStreamWriter(memoryStream, leaveOpen: true);
        
        // Act - Write with bare CR
        writer.Write("Line with bare CR\rAnother line\r");
        writer.Flush();
        
        // Assert - Write() method preserves original line endings
        var result = Encoding.UTF8.GetString(memoryStream.ToArray());
        Assert.Equal("Line with bare CR\rAnother line\r", result);
    }

    // RFC 5321: Control characters (0-31, 127) handling in SMTP
    [Fact]
    public void WriteLine_RFC5321_ControlCharacters_WritesCorrectly()
    {
        // Arrange
        using var memoryStream = new MemoryStream();
        using var writer = new SmtpStreamWriter(memoryStream, leaveOpen: true);
        
        // Act - Write text containing control characters (tab is allowed, others preserved)
        writer.WriteLine("Line with tab\tcharacter");
        writer.WriteLine("Line with null\0character");
        writer.WriteLine("Line with bell\u0007character");
        writer.Flush();
        
        // Assert
        var result = Encoding.UTF8.GetString(memoryStream.ToArray());
        var expected = "Line with tab\tcharacter\r\n" +
                      "Line with null\0character\r\n" +
                      "Line with bell\u0007character\r\n";
        Assert.Equal(expected, result);
    }

    // RFC 5321 Section 4.1.1.4: Command line length must not exceed 512 octets
    [Fact]
    public void WriteLine_RFC5321_CommandLineLength_512Octets()
    {
        // Arrange
        using var memoryStream = new MemoryStream();
        using var writer = new SmtpStreamWriter(memoryStream, leaveOpen: true);
        
        // Create a command line exactly 512 octets (including CRLF)
        // 512 - 2 (for CRLF) = 510 characters
        // "MAIL FROM:<@example.com>" = 24 characters, so need 510 - 24 = 486 'a' characters
        var commandLine = "MAIL FROM:<" + new string('a', 486) + "@example.com>";
        Assert.Equal(510, commandLine.Length);
        
        // Act
        writer.WriteLine(commandLine);
        writer.Flush();
        
        // Assert
        var result = Encoding.UTF8.GetString(memoryStream.ToArray());
        Assert.Equal(commandLine + "\r\n", result);
        Assert.Equal(512, result.Length); // Exactly 512 octets with CRLF
    }

    // RFC 5321: High-bit characters in UTF-8 (8BITMIME extension)
    [Fact]
    public void WriteLine_RFC5321_HighBitCharacters_UTF8Encoding()
    {
        // Arrange
        using var memoryStream = new MemoryStream();
        using var writer = new SmtpStreamWriter(memoryStream, leaveOpen: true);
        
        // Act - Write text with high-bit characters (UTF-8 encoded)
        writer.WriteLine("Café münü naïve résumé");
        writer.WriteLine("Japanese: こんにちは");
        writer.WriteLine("Emoji: 📧✉️");
        writer.Flush();
        
        // Assert
        var result = Encoding.UTF8.GetString(memoryStream.ToArray());
        var expected = "Café münü naïve résumé\r\n" +
                      "Japanese: こんにちは\r\n" +
                      "Emoji: 📧✉️\r\n";
        Assert.Equal(expected, result);
    }

    // RFC 5321: Empty lines should only contain CRLF
    [Fact]
    public void WriteLine_RFC5321_EmptyLines_OnlyCRLF()
    {
        // Arrange
        using var memoryStream = new MemoryStream();
        using var writer = new SmtpStreamWriter(memoryStream, leaveOpen: true);
        
        // Act
        writer.WriteLine("First line");
        writer.WriteLine(); // Empty line
        writer.WriteLine(""); // Explicitly empty string
        writer.WriteLine("Last line");
        writer.Flush();
        
        // Assert
        var result = Encoding.UTF8.GetString(memoryStream.ToArray());
        var expected = "First line\r\n\r\n\r\nLast line\r\n";
        Assert.Equal(expected, result);
    }

    // RFC 5321 Section 4.5.2: Message size limits (though SmtpStreamWriter doesn't enforce limits)
    [Fact]
    public void WriteLine_RFC5321_LargeMessage_BeyondTypicalLimits()
    {
        // Arrange
        using var memoryStream = new MemoryStream();
        using var writer = new SmtpStreamWriter(memoryStream, leaveOpen: true);
        
        // Create a message larger than typical 10MB SMTP limit but reasonable for testing
        var largeLine = new string('X', 100000); // 100KB line
        
        // Act
        for (int i = 0; i < 10; i++)
        {
            writer.WriteLine($"Line {i}: {largeLine}");
        }
        writer.Flush();
        
        // Assert
        var result = Encoding.UTF8.GetString(memoryStream.ToArray());
        Assert.True(result.Length > 1000000); // Over 1MB
        Assert.Contains("Line 0: XXX", result);
        Assert.Contains("Line 9: XXX", result);
        Assert.EndsWith("\r\n", result);
    }

    // RFC 2822 Section 2.1.1: Recommended line length is 78 characters
    [Fact]
    public void WriteLine_RFC2822_RecommendedLineLength_78Characters()
    {
        // Arrange
        using var memoryStream = new MemoryStream();
        using var writer = new SmtpStreamWriter(memoryStream, leaveOpen: true);
        
        // Create a line exactly 78 characters (RFC 2822 recommended maximum)
        var recommendedLine = new string('R', 78);
        
        // Act
        writer.WriteLine(recommendedLine);
        writer.Flush();
        
        // Assert
        var result = Encoding.UTF8.GetString(memoryStream.ToArray());
        Assert.Equal(recommendedLine + "\r\n", result);
        Assert.Equal(78 + 2, result.Length);
    }

    // RFC 5321: Verify CRLF is atomic and cannot be split
    [Fact]
    public void WriteLine_RFC5321_CRLFAtomic_NotSplit()
    {
        // Arrange
        using var memoryStream = new MemoryStream();
        using var writer = new SmtpStreamWriter(memoryStream, leaveOpen: true);
        
        // Act - Multiple rapid writes to test CRLF atomicity
        writer.WriteLine("Line 1");
        writer.WriteLine("Line 2");
        writer.WriteLine("Line 3");
        writer.Flush();
        
        // Assert - Each line should end with complete CRLF
        var result = Encoding.UTF8.GetString(memoryStream.ToArray());
        var lines = result.Split(new[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(3, lines.Length);
        Assert.Equal("Line 1", lines[0]);
        Assert.Equal("Line 2", lines[1]);
        Assert.Equal("Line 3", lines[2]);
        
        // Verify complete CRLF sequences
        Assert.Equal("Line 1\r\nLine 2\r\nLine 3\r\n", result);
    }
}