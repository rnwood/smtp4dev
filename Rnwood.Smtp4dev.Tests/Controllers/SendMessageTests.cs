using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using AwesomeAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MimeKit;
using NSubstitute;
using Rnwood.Smtp4dev.Controllers;
using Rnwood.Smtp4dev.Data;
using Rnwood.Smtp4dev.Server;
using Xunit;

namespace Rnwood.Smtp4dev.Tests.Controllers
{
    public class SendMessageTests
    {
        private readonly MessagesController controller;
        private readonly ISmtp4devServer server;

        public SendMessageTests()
        {
            var messagesRepository = Substitute.For<IMessagesRepository>();
            server = Substitute.For<ISmtp4devServer>();
            controller = new MessagesController(messagesRepository, server, new MimeProcessingService());
        }

        private void SetupHttpContext(string contentType, byte[] body = null)
        {
            var httpContext = new DefaultHttpContext();
            httpContext.Request.ContentType = contentType;
            if (body != null)
            {
                httpContext.Request.Body = new MemoryStream(body);
            }
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };
        }

        [Fact]
        public async Task Send_WithCustomHeadersJson_PassesHeadersToServer()
        {
            // Arrange
            SetupHttpContext("multipart/form-data");
            string headersJson = "{\"X-Custom-Header\": \"custom-value\", \"X-Another\": \"another-value\"}";

            // Act
            var result = await controller.Send(
                to: "to@example.com",
                cc: null,
                bcc: null,
                from: "from@example.com",
                deliverToAll: false,
                subject: "Test Subject",
                bodyHtml: "<html>Test</html>",
                headers: headersJson,
                attachments: null);

            // Assert
            result.Should().BeOfType<OkResult>();
            server.Received(1).Send(
                Arg.Is<IDictionary<string, string>>(h =>
                    h["X-Custom-Header"] == "custom-value" &&
                    h["X-Another"] == "another-value"),
                Arg.Any<string[]>(),
                Arg.Any<string[]>(),
                "from@example.com",
                Arg.Any<string[]>(),
                "Test Subject",
                "<html>Test</html>",
                Arg.Any<IEnumerable<AttachmentInfo>>());
        }

        [Fact]
        public async Task Send_WithInvalidHeadersJson_ReturnsBadRequest()
        {
            // Arrange
            SetupHttpContext("multipart/form-data");
            string invalidJson = "not-valid-json";

            // Act
            var result = await controller.Send(
                to: "to@example.com",
                cc: null,
                bcc: null,
                from: "from@example.com",
                deliverToAll: false,
                subject: "Test Subject",
                bodyHtml: "<html>Test</html>",
                headers: invalidJson,
                attachments: null);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
            server.DidNotReceive().Send(
                Arg.Any<IDictionary<string, string>>(),
                Arg.Any<string[]>(),
                Arg.Any<string[]>(),
                Arg.Any<string>(),
                Arg.Any<string[]>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<IEnumerable<AttachmentInfo>>());
        }

        [Fact]
        public async Task Send_WithRawEml_CallsSendRaw()
        {
            // Arrange
            var mimeMessage = new MimeMessage();
            mimeMessage.From.Add(InternetAddress.Parse("sender@example.com"));
            mimeMessage.To.Add(InternetAddress.Parse("recipient@example.com"));
            mimeMessage.Subject = "Raw EML Test";
            var bodyBuilder = new BodyBuilder { HtmlBody = "<html>Hello</html>" };
            mimeMessage.Body = bodyBuilder.ToMessageBody();

            using var emlStream = new MemoryStream();
            await mimeMessage.WriteToAsync(emlStream);
            byte[] emlData = emlStream.ToArray();

            SetupHttpContext("message/rfc822", emlData);

            // Act
            var result = await controller.Send(
                to: null,
                cc: null,
                bcc: null,
                from: null,
                deliverToAll: false,
                subject: null,
                bodyHtml: null,
                headers: null,
                attachments: null);

            // Assert
            result.Should().BeOfType<OkResult>();
            server.Received(1).SendRaw(
                Arg.Is<MimeMessage>(m => m.Subject == "Raw EML Test"),
                "sender@example.com",
                Arg.Is<string[]>(r => r.Length == 1 && r[0] == "recipient@example.com"));
        }

        [Fact]
        public async Task Send_WithRawEmlAndOverrideRecipients_UsesQueryParamRecipients()
        {
            // Arrange
            var mimeMessage = new MimeMessage();
            mimeMessage.From.Add(InternetAddress.Parse("sender@example.com"));
            mimeMessage.To.Add(InternetAddress.Parse("original@example.com"));
            mimeMessage.Subject = "Override Recipients Test";
            var bodyBuilder = new BodyBuilder { HtmlBody = "<html>Hello</html>" };
            mimeMessage.Body = bodyBuilder.ToMessageBody();

            using var emlStream = new MemoryStream();
            await mimeMessage.WriteToAsync(emlStream);
            byte[] emlData = emlStream.ToArray();

            SetupHttpContext("message/rfc822", emlData);

            // Act
            var result = await controller.Send(
                to: "override@example.com",
                cc: null,
                bcc: null,
                from: "override-sender@example.com",
                deliverToAll: false,
                subject: null,
                bodyHtml: null,
                headers: null,
                attachments: null);

            // Assert
            result.Should().BeOfType<OkResult>();
            server.Received(1).SendRaw(
                Arg.Any<MimeMessage>(),
                "override-sender@example.com",
                Arg.Is<string[]>(r => r.Length == 1 && r[0] == "override@example.com"));
        }

        [Fact]
        public async Task Send_WithRawEmlMissingFrom_ReturnsBadRequest()
        {
            // Arrange - EML with no From header
            var mimeMessage = new MimeMessage();
            mimeMessage.To.Add(InternetAddress.Parse("recipient@example.com"));
            mimeMessage.Subject = "No From Test";
            var bodyBuilder = new BodyBuilder { HtmlBody = "<html>Hello</html>" };
            mimeMessage.Body = bodyBuilder.ToMessageBody();

            using var emlStream = new MemoryStream();
            await mimeMessage.WriteToAsync(emlStream);
            byte[] emlData = emlStream.ToArray();

            SetupHttpContext("message/rfc822", emlData);

            // Act
            var result = await controller.Send(
                to: null,
                cc: null,
                bcc: null,
                from: null,
                deliverToAll: false,
                subject: null,
                bodyHtml: null,
                headers: null,
                attachments: null);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
            server.DidNotReceive().SendRaw(
                Arg.Any<MimeMessage>(),
                Arg.Any<string>(),
                Arg.Any<string[]>());
        }

        [Fact]
        public async Task Send_WithRawEmlMissingRecipients_ReturnsBadRequest()
        {
            // Arrange - EML with no recipients
            var mimeMessage = new MimeMessage();
            mimeMessage.From.Add(InternetAddress.Parse("sender@example.com"));
            mimeMessage.Subject = "No Recipients Test";
            var bodyBuilder = new BodyBuilder { HtmlBody = "<html>Hello</html>" };
            mimeMessage.Body = bodyBuilder.ToMessageBody();

            using var emlStream = new MemoryStream();
            await mimeMessage.WriteToAsync(emlStream);
            byte[] emlData = emlStream.ToArray();

            SetupHttpContext("message/rfc822", emlData);

            // Act
            var result = await controller.Send(
                to: null,
                cc: null,
                bcc: null,
                from: null,
                deliverToAll: false,
                subject: null,
                bodyHtml: null,
                headers: null,
                attachments: null);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
            server.DidNotReceive().SendRaw(
                Arg.Any<MimeMessage>(),
                Arg.Any<string>(),
                Arg.Any<string[]>());
        }

        [Fact]
        public async Task Send_WithRawEmlEmpty_ReturnsBadRequest()
        {
            // Arrange
            SetupHttpContext("message/rfc822", Array.Empty<byte>());

            // Act
            var result = await controller.Send(
                to: null,
                cc: null,
                bcc: null,
                from: null,
                deliverToAll: false,
                subject: null,
                bodyHtml: null,
                headers: null,
                attachments: null);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task Send_WithNoCustomHeaders_SendsEmptyHeaderDictionary()
        {
            // Arrange
            SetupHttpContext("multipart/form-data");

            // Act
            var result = await controller.Send(
                to: "to@example.com",
                cc: null,
                bcc: null,
                from: "from@example.com",
                deliverToAll: false,
                subject: "Test Subject",
                bodyHtml: "<html>Test</html>",
                headers: null,
                attachments: null);

            // Assert
            result.Should().BeOfType<OkResult>();
            server.Received(1).Send(
                Arg.Is<IDictionary<string, string>>(h => h.Count == 0),
                Arg.Any<string[]>(),
                Arg.Any<string[]>(),
                Arg.Any<string>(),
                Arg.Any<string[]>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<IEnumerable<AttachmentInfo>>());
        }
    }
}
