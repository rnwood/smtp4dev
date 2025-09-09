# Using smtp4dev in Automated Tests

This guide covers different ways to integrate smtp4dev into your automated testing workflows for testing email functionality.

## Table of Contents

- [Running smtp4dev and Using the API](#running-smtp4dev-and-using-the-api)
- [Using SignalR for Real-time Updates](#using-signalr-for-real-time-updates)
- [Testcontainers Examples](#testcontainers-examples)
- [Direct SMTP Server Component Usage (.NET)](#direct-smtp-server-component-usage-net)

## Running smtp4dev and Using the API

### Starting smtp4dev Programmatically

You can start smtp4dev as a separate process and interact with it via the REST API. This approach works with any programming language and testing framework.

#### .NET Example

```csharp
using System;
using System.Diagnostics;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

public class EmailIntegrationTests : IDisposable
{
    private Process smtp4devProcess;
    private HttpClient httpClient;
    private readonly string baseUrl = "http://localhost:5000";
    private readonly int smtpPort = 2525;

    public EmailIntegrationTests()
    {
        // Start smtp4dev process
        smtp4devProcess = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"run --project path/to/Rnwood.Smtp4dev.csproj --urls={baseUrl} --smtpport={smtpPort} --imapport=1143 --db= --nousersettings",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            }
        };
        
        smtp4devProcess.Start();
        
        // Wait for smtp4dev to start (you may want to implement a more robust waiting mechanism)
        Task.Delay(5000).Wait();
        
        httpClient = new HttpClient { BaseAddress = new Uri(baseUrl) };
    }

    [Fact]
    public async Task SendEmail_ShouldAppearInAPI()
    {
        // Send email via SMTP
        using var smtpClient = new System.Net.Mail.SmtpClient("localhost", smtpPort);
        var message = new System.Net.Mail.MailMessage(
            from: "test@example.com",
            to: "recipient@example.com",
            subject: "Test Email",
            body: "This is a test email");
        
        await smtpClient.SendMailAsync(message);

        // Wait a moment for processing
        await Task.Delay(1000);

        // Retrieve messages via API
        var response = await httpClient.GetAsync("/api/messages");
        response.EnsureSuccessStatusCode();
        
        var json = await response.Content.ReadAsStringAsync();
        var messages = JsonSerializer.Deserialize<MessageSummary[]>(json);
        
        Assert.NotEmpty(messages);
        Assert.Equal("Test Email", messages[0].Subject);
    }

    public void Dispose()
    {
        httpClient?.Dispose();
        smtp4devProcess?.Kill();
        smtp4devProcess?.Dispose();
    }
}

public class MessageSummary
{
    public string Subject { get; set; }
    public string From { get; set; }
    public string To { get; set; }
    public DateTime ReceivedDate { get; set; }
}
```

#### Node.js/JavaScript Example

```javascript
const { spawn } = require('child_process');
const axios = require('axios');
const nodemailer = require('nodemailer');

describe('Email Integration Tests', () => {
    let smtp4devProcess;
    const baseUrl = 'http://localhost:5000';
    const smtpPort = 2525;

    beforeAll(async () => {
        // Start smtp4dev
        smtp4devProcess = spawn('dotnet', [
            'run', 
            '--project', 'path/to/Rnwood.Smtp4dev.csproj',
            '--urls', baseUrl,
            '--smtpport', smtpPort.toString(),
            '--imapport', '1143',
            '--db', '',
            '--nousersettings'
        ]);

        // Wait for startup
        await new Promise(resolve => setTimeout(resolve, 5000));
    });

    afterAll(() => {
        if (smtp4devProcess) {
            smtp4devProcess.kill();
        }
    });

    test('should capture sent email', async () => {
        // Send email via SMTP
        const transporter = nodemailer.createTransporter({
            host: 'localhost',
            port: smtpPort,
            secure: false
        });

        await transporter.sendMail({
            from: 'test@example.com',
            to: 'recipient@example.com',
            subject: 'Test Email',
            text: 'This is a test email'
        });

        // Wait for processing
        await new Promise(resolve => setTimeout(resolve, 1000));

        // Check API
        const response = await axios.get(`${baseUrl}/api/messages`);
        expect(response.data).toHaveLength(1);
        expect(response.data[0].subject).toBe('Test Email');
    });
});
```

#### Python Example

```python
import subprocess
import time
import requests
import smtplib
from email.mime.text import MIMEText
import unittest

class EmailIntegrationTests(unittest.TestCase):
    
    def setUp(self):
        # Start smtp4dev
        self.smtp4dev_process = subprocess.Popen([
            'dotnet', 'run',
            '--project', 'path/to/Rnwood.Smtp4dev.csproj',
            '--urls', 'http://localhost:5000',
            '--smtpport', '2525',
            '--imapport', '1143',
            '--db', '',
            '--nousersettings'
        ])
        
        # Wait for startup
        time.sleep(5)
        
        self.base_url = 'http://localhost:5000'
        self.smtp_port = 2525

    def tearDown(self):
        if self.smtp4dev_process:
            self.smtp4dev_process.terminate()

    def test_send_email_appears_in_api(self):
        # Send email via SMTP
        msg = MIMEText('This is a test email')
        msg['Subject'] = 'Test Email'
        msg['From'] = 'test@example.com'
        msg['To'] = 'recipient@example.com'

        with smtplib.SMTP('localhost', self.smtp_port) as smtp:
            smtp.send_message(msg)

        # Wait for processing
        time.sleep(1)

        # Check API
        response = requests.get(f'{self.base_url}/api/messages')
        messages = response.json()
        
        self.assertEqual(len(messages), 1)
        self.assertEqual(messages[0]['subject'], 'Test Email')
```

### API Endpoints

The smtp4dev API provides several useful endpoints for testing:

- `GET /api/messages` - Get all messages
- `GET /api/messages/{id}` - Get specific message details
- `GET /api/messages/{id}/source` - Get raw message source
- `GET /api/messages/{id}/part/{partid}/content` - Get message part content
- `DELETE /api/messages` - Delete all messages
- `DELETE /api/messages/{id}` - Delete specific message
- `GET /api/sessions` - Get SMTP sessions
- `DELETE /api/sessions` - Delete all sessions

For complete API documentation, visit `<smtp4devurl>/api` when smtp4dev is running.

## Using SignalR for Real-time Updates

smtp4dev provides SignalR notifications for real-time updates when messages arrive.

### .NET SignalR Client Example

```csharp
using Microsoft.AspNetCore.SignalR.Client;
using System;
using System.Threading.Tasks;

public class RealtimeEmailMonitor : IAsyncDisposable
{
    private HubConnection connection;

    public async Task StartAsync(string baseUrl)
    {
        connection = new HubConnectionBuilder()
            .WithUrl($"{baseUrl}/hubs/notifications")
            .Build();

        // Subscribe to message notifications
        connection.On<string>("messageschanged", mailbox =>
        {
            Console.WriteLine($"New messages in mailbox: {mailbox}");
            // Handle new messages
        });

        // Subscribe to session notifications
        connection.On("sessionschanged", () =>
        {
            Console.WriteLine("SMTP sessions changed");
            // Handle session changes
        });

        await connection.StartAsync();
    }

    public async ValueTask DisposeAsync()
    {
        if (connection != null)
        {
            await connection.DisposeAsync();
        }
    }
}

// Usage in tests
[Fact]
public async Task Test_RealtimeNotifications()
{
    var monitor = new RealtimeEmailMonitor();
    await monitor.StartAsync("http://localhost:5000");

    // Send email and wait for notification
    // Your email sending code here
    
    await monitor.DisposeAsync();
}
```

### JavaScript SignalR Client Example

```javascript
const signalR = require('@microsoft/signalr');

class RealtimeEmailMonitor {
    constructor(baseUrl) {
        this.connection = new signalR.HubConnectionBuilder()
            .withUrl(`${baseUrl}/hubs/notifications`)
            .build();
    }

    async start() {
        // Subscribe to notifications
        this.connection.on('messageschanged', (mailbox) => {
            console.log(`New messages in mailbox: ${mailbox}`);
        });

        this.connection.on('sessionschanged', () => {
            console.log('SMTP sessions changed');
        });

        await this.connection.start();
    }

    async stop() {
        await this.connection.stop();
    }
}

// Usage in tests
test('should receive real-time notifications', async () => {
    const monitor = new RealtimeEmailMonitor('http://localhost:5000');
    await monitor.start();

    // Send email and wait for notification
    // Your test code here

    await monitor.stop();
});
```

## Testcontainers Examples

Testcontainers provides a clean way to run smtp4dev in containers for testing across multiple languages.

### .NET Testcontainers

First, install the Testcontainers NuGet package:

```xml
<PackageReference Include="Testcontainers" Version="3.0.0" />
```

```csharp
using Testcontainers.Containers;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

public class SmtpTestcontainersTests : IAsyncLifetime
{
    private IContainer smtp4devContainer;
    private HttpClient httpClient;
    private string baseUrl;
    private int smtpPort;

    public async Task InitializeAsync()
    {
        smtp4devContainer = new ContainerBuilder()
            .WithImage("rnwood/smtp4dev:v3")
            .WithPortBinding(80, true)
            .WithPortBinding(25, true)
            .WithPortBinding(143, true)
            .WithEnvironment("ServerOptions__Urls", "http://*:80")
            .WithWaitStrategy(Wait.ForUnixContainer().UntilHttpRequestIsSucceeded(r => r.ForPort(80)))
            .Build();

        await smtp4devContainer.StartAsync();

        var webPort = smtp4devContainer.GetMappedPublicPort(80);
        smtpPort = smtp4devContainer.GetMappedPublicPort(25);
        baseUrl = $"http://localhost:{webPort}";
        
        httpClient = new HttpClient { BaseAddress = new Uri(baseUrl) };
    }

    [Fact]
    public async Task TestEmailCapture()
    {
        // Send email via SMTP
        using var smtpClient = new System.Net.Mail.SmtpClient("localhost", smtpPort);
        var message = new System.Net.Mail.MailMessage(
            "test@example.com", 
            "recipient@example.com", 
            "Test Subject", 
            "Test Body");
        
        await smtpClient.SendMailAsync(message);

        // Wait and check API
        await Task.Delay(1000);
        var response = await httpClient.GetAsync("/api/messages");
        var content = await response.Content.ReadAsStringAsync();
        
        Assert.Contains("Test Subject", content);
    }

    public async Task DisposeAsync()
    {
        httpClient?.Dispose();
        if (smtp4devContainer != null)
        {
            await smtp4devContainer.DisposeAsync();
        }
    }
}
```

### Java Testcontainers

Add the Testcontainers dependency to your `pom.xml`:

```xml
<dependency>
    <groupId>org.testcontainers</groupId>
    <artifactId>testcontainers</artifactId>
    <version>1.19.0</version>
    <scope>test</scope>
</dependency>
```

```java
import org.testcontainers.containers.GenericContainer;
import org.testcontainers.containers.wait.strategy.Wait;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.AfterEach;

import javax.mail.*;
import javax.mail.internet.InternetAddress;
import javax.mail.internet.MimeMessage;
import java.net.http.HttpClient;
import java.net.http.HttpRequest;
import java.net.http.HttpResponse;
import java.net.URI;
import java.util.Properties;

public class SmtpTestcontainersTest {
    
    private GenericContainer<?> smtp4dev;
    private HttpClient httpClient;
    private String baseUrl;
    private int smtpPort;

    @BeforeEach
    void setUp() {
        smtp4dev = new GenericContainer<>("rnwood/smtp4dev:v3")
                .withExposedPorts(80, 25, 143)
                .withEnv("ServerOptions__Urls", "http://*:80")
                .waitingFor(Wait.forHttp("/").forPort(80));
        
        smtp4dev.start();
        
        int webPort = smtp4dev.getMappedPort(80);
        smtpPort = smtp4dev.getMappedPort(25);
        baseUrl = "http://localhost:" + webPort;
        
        httpClient = HttpClient.newHttpClient();
    }

    @Test
    void testEmailCapture() throws Exception {
        // Send email via SMTP
        Properties props = new Properties();
        props.put("mail.smtp.host", "localhost");
        props.put("mail.smtp.port", smtpPort);
        
        Session session = Session.getDefaultInstance(props);
        MimeMessage message = new MimeMessage(session);
        message.setFrom(new InternetAddress("test@example.com"));
        message.addRecipient(Message.RecipientType.TO, new InternetAddress("recipient@example.com"));
        message.setSubject("Test Subject");
        message.setText("Test Body");
        
        Transport.send(message);
        
        // Wait and check API
        Thread.sleep(1000);
        HttpRequest request = HttpRequest.newBuilder()
                .uri(URI.create(baseUrl + "/api/messages"))
                .build();
        
        HttpResponse<String> response = httpClient.send(request, HttpResponse.BodyHandlers.ofString());
        assert response.body().contains("Test Subject");
    }

    @AfterEach
    void tearDown() {
        if (smtp4dev != null) {
            smtp4dev.stop();
        }
    }
}
```

### Python Testcontainers

Install the testcontainers package:

```bash
pip install testcontainers
```

```python
import unittest
import time
import requests
import smtplib
from email.mime.text import MIMEText
from testcontainers.generic import GenericContainer

class SmtpTestcontainersTests(unittest.TestCase):
    
    def setUp(self):
        self.smtp4dev = GenericContainer("rnwood/smtp4dev:v3") \
            .with_exposed_ports(80, 25, 143) \
            .with_env("ServerOptions__Urls", "http://*:80")
        
        self.smtp4dev.start()
        
        self.web_port = self.smtp4dev.get_exposed_port(80)
        self.smtp_port = self.smtp4dev.get_exposed_port(25)
        self.base_url = f"http://localhost:{self.web_port}"

    def tearDown(self):
        self.smtp4dev.stop()

    def test_email_capture(self):
        # Send email via SMTP
        msg = MIMEText('Test Body')
        msg['Subject'] = 'Test Subject'
        msg['From'] = 'test@example.com'
        msg['To'] = 'recipient@example.com'

        with smtplib.SMTP('localhost', self.smtp_port) as smtp:
            smtp.send_message(msg)

        # Wait and check API
        time.sleep(1)
        response = requests.get(f'{self.base_url}/api/messages')
        self.assertIn('Test Subject', response.text)
```

### Go Testcontainers

```go
package main

import (
    "context"
    "fmt"
    "net/http"
    "net/smtp"
    "testing"
    "time"
    
    "github.com/testcontainers/testcontainers-go"
    "github.com/testcontainers/testcontainers-go/wait"
)

func TestSmtpWithTestcontainers(t *testing.T) {
    ctx := context.Background()
    
    req := testcontainers.ContainerRequest{
        Image:        "rnwood/smtp4dev:v3",
        ExposedPorts: []string{"80/tcp", "25/tcp", "143/tcp"},
        Env: map[string]string{
            "ServerOptions__Urls": "http://*:80",
        },
        WaitingFor: wait.ForHTTP("/").WithPort("80/tcp"),
    }
    
    container, err := testcontainers.GenericContainer(ctx, testcontainers.GenericContainerRequest{
        ContainerRequest: req,
        Started:          true,
    })
    if err != nil {
        t.Fatal(err)
    }
    defer container.Terminate(ctx)
    
    webPort, err := container.MappedPort(ctx, "80")
    if err != nil {
        t.Fatal(err)
    }
    
    smtpPort, err := container.MappedPort(ctx, "25")
    if err != nil {
        t.Fatal(err)
    }
    
    baseURL := fmt.Sprintf("http://localhost:%s", webPort.Port())
    
    // Send email via SMTP
    auth := smtp.PlainAuth("", "", "", "localhost")
    to := []string{"recipient@example.com"}
    msg := []byte("To: recipient@example.com\r\n" +
        "Subject: Test Subject\r\n" +
        "\r\n" +
        "Test Body\r\n")
    
    err = smtp.SendMail(fmt.Sprintf("localhost:%s", smtpPort.Port()), auth, "test@example.com", to, msg)
    if err != nil {
        t.Fatal(err)
    }
    
    // Wait and check API
    time.Sleep(1 * time.Second)
    resp, err := http.Get(baseURL + "/api/messages")
    if err != nil {
        t.Fatal(err)
    }
    defer resp.Body.Close()
    
    // Check response contains expected content
    // Add your assertions here
}
```

## Direct SMTP Server Component Usage (.NET)

For .NET applications, you can use the SMTP server component directly without running the full smtp4dev application.

### Installing the SMTP Server Package

Add the SMTP server package to your test project:

```xml
<PackageReference Include="Rnwood.SmtpServer" Version="[latest-version]" />
```

### Basic Usage

```csharp
using Rnwood.SmtpServer;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Net.Mail;
using System.Security.Authentication;
using System.Threading.Tasks;
using Xunit;

public class DirectSmtpServerTests : IDisposable
{
    private SmtpServer smtpServer;
    private ConcurrentBag<IMessage> receivedMessages;

    public DirectSmtpServerTests()
    {
        receivedMessages = new ConcurrentBag<IMessage>();
        
        // Create SMTP server with automatic port assignment
        var options = new ServerOptions(
            allowRemoteConnections: false,
            enableIpV6: false,
            domainName: "localhost",
            portNumber: (int)StandardSmtpPort.AssignAutomatically,
            requireAuthentication: false,
            nonSecureAuthMechanismIds: Array.Empty<string>(),
            secureAuthMechanismNamesIds: Array.Empty<string>(),
            implcitTlsCertificate: null,
            startTlsCertificate: null,
            sslProtocols: SslProtocols.None,
            tlsCipherSuites: null,
            maxMessageSize: null
        );

        smtpServer = new SmtpServer(options);
        
        // Subscribe to message received events
        smtpServer.MessageReceivedEventHandler += (sender, args) =>
        {
            receivedMessages.Add(args.Message);
            return Task.CompletedTask;
        };
        
        smtpServer.Start();
    }

    [Fact]
    public async Task SendEmail_ShouldBeReceived()
    {
        var port = smtpServer.ListeningEndpoints.First().Port;
        
        // Send email via System.Net.Mail
        using var smtpClient = new SmtpClient("localhost", port);
        var message = new MailMessage(
            from: "sender@example.com",
            to: "recipient@example.com",
            subject: "Test Email",
            body: "This is a test message");
        
        await smtpClient.SendMailAsync(message);
        
        // Verify message was received
        Assert.Single(receivedMessages);
        var receivedMessage = receivedMessages.First();
        Assert.Equal("sender@example.com", receivedMessage.From);
        Assert.Contains("recipient@example.com", receivedMessage.Recipients.Select(r => r.ToString()));
    }

    [Fact]
    public async Task SendMultipleEmails_ShouldReceiveAll()
    {
        var port = smtpServer.ListeningEndpoints.First().Port;
        
        using var smtpClient = new SmtpClient("localhost", port);
        
        // Send multiple emails
        for (int i = 0; i < 5; i++)
        {
            var message = new MailMessage(
                from: $"sender{i}@example.com",
                to: "recipient@example.com",
                subject: $"Test Email {i}",
                body: $"Message content {i}");
            
            await smtpClient.SendMailAsync(message);
        }
        
        // Verify all messages were received
        Assert.Equal(5, receivedMessages.Count);
    }

    public void Dispose()
    {
        smtpServer?.Stop();
        smtpServer?.Dispose();
    }
}
```

### Advanced Usage with MailKit

```csharp
using MailKit.Net.Smtp;
using MimeKit;
using Rnwood.SmtpServer;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Security.Authentication;
using System.Threading.Tasks;
using Xunit;

public class AdvancedSmtpServerTests : IDisposable
{
    private SmtpServer smtpServer;
    private ConcurrentBag<IMessage> receivedMessages;
    private ConcurrentBag<ISession> sessions;

    public AdvancedSmtpServerTests()
    {
        receivedMessages = new ConcurrentBag<IMessage>();
        sessions = new ConcurrentBag<ISession>();
        
        var options = new ServerOptions(
            allowRemoteConnections: false,
            enableIpV6: false,
            domainName: "localhost",
            portNumber: (int)StandardSmtpPort.AssignAutomatically,
            requireAuthentication: false,
            nonSecureAuthMechanismIds: Array.Empty<string>(),
            secureAuthMechanismNamesIds: Array.Empty<string>(),
            implcitTlsCertificate: null,
            startTlsCertificate: null,
            sslProtocols: SslProtocols.None,
            tlsCipherSuites: null,
            maxMessageSize: null
        );

        smtpServer = new SmtpServer(options);
        
        smtpServer.MessageReceivedEventHandler += (sender, args) =>
        {
            receivedMessages.Add(args.Message);
            return Task.CompletedTask;
        };
        
        smtpServer.SessionStartedHandler += (sender, args) =>
        {
            sessions.Add(args.Session);
            return Task.CompletedTask;
        };
        
        smtpServer.Start();
    }

    [Fact]
    public async Task SendComplexEmail_ShouldReceiveWithAttachments()
    {
        var port = smtpServer.ListeningEndpoints.First().Port;
        
        // Create complex email with MailKit
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress("Test Sender", "sender@example.com"));
        message.To.Add(new MailboxAddress("Test Recipient", "recipient@example.com"));
        message.Subject = "Complex Test Email";

        var bodyBuilder = new BodyBuilder
        {
            TextBody = "This is the plain text body",
            HtmlBody = "<p>This is the <b>HTML</b> body</p>"
        };
        
        // Add attachment
        bodyBuilder.Attachments.Add("test.txt", System.Text.Encoding.UTF8.GetBytes("Test attachment content"));
        message.Body = bodyBuilder.ToMessageBody();

        using var smtpClient = new SmtpClient();
        await smtpClient.ConnectAsync("localhost", port, false);
        await smtpClient.SendAsync(message);
        await smtpClient.DisconnectAsync(true);
        
        // Verify message structure
        Assert.Single(receivedMessages);
        var receivedMessage = receivedMessages.First();
        
        // Parse the received message
        var parsedMessage = MimeMessage.Load(receivedMessage.Data);
        Assert.Equal("Complex Test Email", parsedMessage.Subject);
        Assert.True(parsedMessage.Body is Multipart);
    }

    [Fact]
    public void SessionTracking_ShouldCaptureConnections()
    {
        var port = smtpServer.ListeningEndpoints.First().Port;
        
        // Make multiple connections
        for (int i = 0; i < 3; i++)
        {
            using var smtpClient = new System.Net.Mail.SmtpClient("localhost", port);
            var message = new System.Net.Mail.MailMessage(
                "test@example.com", 
                "recipient@example.com", 
                $"Message {i}", 
                "Body");
            smtpClient.Send(message);
        }
        
        // Verify sessions were tracked
        Assert.Equal(3, sessions.Count);
    }

    public void Dispose()
    {
        smtpServer?.Stop();
        smtpServer?.Dispose();
    }
}
```

## Best Practices

1. **Use unique ports**: Always use port 0 or specific non-standard ports to avoid conflicts
2. **Clean up resources**: Properly dispose of SMTP servers, containers, and HTTP clients
3. **Wait for processing**: Add small delays after sending emails to ensure they're processed
4. **Use in-memory databases**: For faster tests, use `--db=` to use in-memory storage
5. **Disable settings persistence**: Use `--nousersettings` to avoid persisting test configurations
6. **Monitor logs**: Enable debug logging to troubleshoot issues during test development

## Troubleshooting

### Common Issues

1. **Port conflicts**: Ensure ports are available or use port 0 for automatic assignment
2. **Timing issues**: Add appropriate delays after sending emails before checking results
3. **Container networking**: Ensure containers can communicate with test runners
4. **Resource cleanup**: Always dispose of containers and processes in teardown methods

### Debug Logging

Enable verbose logging by adding `--debugsettings` to smtp4dev arguments or setting appropriate log levels in your container environment.

## Additional Resources

- [smtp4dev API Documentation](API.md)
- [Docker Configuration](Configuration.md)
- [Getting Started Guide](Getting-Started.md)