# Rnwood.SmtpServer

A .NET SMTP server component, as used by Smtp4dev.

[![Build status](https://ci.appveyor.com/api/projects/status/tay9sajnfh4vy2x0/branch/master?svg=true)](https://ci.appveyor.com/project/rnwood/smtpserver/branch/master)
[![FOSSA Status](https://app.fossa.com/api/projects/git%2Bgithub.com%2Frnwood%2Fsmtpserver.svg?type=shield)](https://app.fossa.com/projects/git%2Bgithub.com%2Frnwood%2Fsmtpserver?ref=badge_shield)

## Usage

### Creating a Server with the Fluent Builder API

The recommended way to create server options is using the fluent builder API:

```csharp
using Rnwood.SmtpServer;

// Create server options using the builder
var options = ServerOptions.Builder()
    .WithDomainName("smtp.example.com")
    .WithPort(25)
    .WithAllowRemoteConnections(true)
    .WithEnableIpV6(false)
    .Build();

// Create and start the server
using var server = new SmtpServer(options);
server.Start();
```

### Builder Methods

The `ServerOptionsBuilder` provides the following methods:

- `WithDomainName(string)` - Set the domain name in server greeting
- `WithPort(int)` - Set the TCP port number
- `WithAllowRemoteConnections(bool)` - Allow/disallow remote connections
- `WithEnableIpV6(bool)` - Enable/disable IPv6 dual stack
- `WithRequireAuthentication(bool)` - Require authentication
- `WithNonSecureAuthMechanisms(params string[])` - Set auth mechanisms for non-secure connections
- `WithSecureAuthMechanisms(params string[])` - Set auth mechanisms for secure connections
- `WithImplicitTlsCertificate(X509Certificate)` - Set certificate for implicit TLS
- `WithStartTlsCertificate(X509Certificate)` - Set certificate for STARTTLS
- `WithSslProtocols(SslProtocols)` - Set allowed SSL/TLS protocol versions
- `WithTlsCipherSuites(params TlsCipherSuite[])` - Set allowed TLS cipher suites
- `WithMaxMessageSize(long?)` - Set maximum message size in bytes
- `WithBindAddress(IPAddress)` - Bind to specific IP address
- `Build()` - Build the ServerOptions instance

### Examples

#### Basic Local Development Server

```csharp
var options = ServerOptions.Builder()
    .WithDomainName("localhost")
    .WithPort(2525)
    .WithAllowRemoteConnections(false)
    .Build();

using var server = new SmtpServer(options);
server.Start();
```

#### Secure Server with TLS

```csharp
var certificate = new X509Certificate2("server.pfx", "password");

var options = ServerOptions.Builder()
    .WithDomainName("secure.example.com")
    .WithPort(465)
    .WithAllowRemoteConnections(true)
    .WithImplicitTlsCertificate(certificate)
    .WithSslProtocols(SslProtocols.Tls12 | SslProtocols.Tls13)
    .Build();

using var server = new SmtpServer(options);
server.Start();
```

#### Server with Authentication

```csharp
var options = ServerOptions.Builder()
    .WithDomainName("smtp.example.com")
    .WithPort(587)
    .WithRequireAuthentication(true)
    .WithSecureAuthMechanisms("PLAIN", "LOGIN")
    .WithStartTlsCertificate(certificate)
    .Build();

using var server = new SmtpServer(options);

// Set up authentication handler
((ServerOptions)server.Options).AuthenticationCredentialsValidationRequiredEventHandler += 
    async (sender, e) => 
    {
        e.AuthenticationResult = ValidateCredentials(e.Credentials) 
            ? AuthenticationResult.Success 
            : AuthenticationResult.Failure;
    };

server.Start();
```

## License
[![FOSSA Status](https://app.fossa.com/api/projects/git%2Bgithub.com%2Frnwood%2Fsmtpserver.svg?type=large)](https://app.fossa.com/projects/git%2Bgithub.com%2Frnwood%2Fsmtpserver?ref=badge_large)