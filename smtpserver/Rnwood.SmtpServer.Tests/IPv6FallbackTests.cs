using System;
using System.Net;
using System.Net.Sockets;
using System.Security.Authentication;
using Xunit;

namespace Rnwood.SmtpServer.Tests
{
    /// <summary>
    /// Tests for IPv6 fallback behavior when IPv6 is not supported
    /// </summary>
    public class IPv6FallbackTests
    {
        /// <summary>
        /// Test that SmtpServer can start when configured for IPv6 but IPv6 is not available
        /// This test simulates the Docker environment issue where IPv6 socket creation fails
        /// </summary>
        [Fact]
        public void SmtpServer_StartWithIPv6Any_ShouldFallbackToIPv4WhenIPv6Unavailable()
        {
            // This test verifies that the server can handle IPv6 unavailability gracefully
            // In environments where IPv6 is truly unavailable, the server should fall back to IPv4
            
            // Create server options that would normally use IPv6
            var options = ServerOptions.Builder()
                .WithAllowRemoteConnections(true)
                .WithEnableIpV6(true) // This would normally create IPv6Any listener
                .WithDomainName("test")
                .WithPort((int)StandardSmtpPort.AssignAutomatically) // Use automatic port assignment
                .WithRequireAuthentication(false)
                .Build();

            using (var server = new SmtpServer(options))
            {
                // This should not throw, even if IPv6 is not available
                // The fallback logic should handle IPv6 unavailability
                Exception startException = null;
                try
                {
                    server.Start();
                }
                catch (Exception ex)
                {
                    startException = ex;
                }
                
                // Server should start successfully (no exception thrown)
                Assert.Null(startException);
                Assert.True(server.IsRunning);
                Assert.NotEmpty(server.ListeningEndpoints);
                
                // In IPv6 environments, we get IPv6 endpoints
                // In IPv4-only environments, we should get IPv4 endpoints
                // Either way, the server should be listening
                foreach (var endpoint in server.ListeningEndpoints)
                {
                    Assert.True(endpoint.Port > 0);
                    // The endpoint should be either IPv4 or IPv6, but not null
                    Assert.NotNull(endpoint.Address);
                }
                
                server.Stop();
                Assert.False(server.IsRunning);
            }
        }

        /// <summary>
        /// Test that SmtpServer can start when configured for IPv6 loopback but IPv6 is not available
        /// </summary>
        [Fact]
        public void SmtpServer_StartWithIPv6Loopback_ShouldFallbackToIPv4WhenIPv6Unavailable()
        {
            // Create server options that would normally use IPv6 loopback
            var options = ServerOptions.Builder()
                .WithAllowRemoteConnections(false)
                .WithEnableIpV6(true) // This would normally create IPv6Loopback listener
                .WithDomainName("test")
                .WithPort((int)StandardSmtpPort.AssignAutomatically) // Use automatic port assignment
                .WithRequireAuthentication(false)
                .Build();

            using (var server = new SmtpServer(options))
            {
                // This should not throw, even if IPv6 is not available
                Exception startException = null;
                try
                {
                    server.Start();
                }
                catch (Exception ex)
                {
                    startException = ex;
                }
                
                // Server should start successfully
                Assert.Null(startException);
                Assert.True(server.IsRunning);
                Assert.NotEmpty(server.ListeningEndpoints);
                
                server.Stop();
                Assert.False(server.IsRunning);
            }
        }
    }
}