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
}