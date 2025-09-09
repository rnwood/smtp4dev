# API Reference

An API is available to allow you to integrate smtp4dev into automated tests and external applications.

## Quick Start

To see the complete API documentation with interactive examples, visit `<smtp4devurl>/api` when smtp4dev is running.

## Using smtp4dev in Automated Tests

For comprehensive examples of integrating smtp4dev into your automated testing workflows, including:

- Running smtp4dev programmatically and using the REST API
- Using SignalR for real-time email notifications  
- Testcontainers examples for multiple programming languages
- Direct SMTP server component usage in .NET

See the **[Testing Guide](Testing.md)** for detailed examples and best practices.

## API Endpoints Overview

The main API endpoints include:

- **Messages**: `/api/messages` - Retrieve, view, and manage captured emails
- **Sessions**: `/api/sessions` - View SMTP session logs and connection details  
- **Mailboxes**: `/api/mailboxes` - Manage virtual mailboxes
- **Server**: `/api/server` - Server status and configuration

For complete endpoint documentation with request/response examples, visit `/api` on your running smtp4dev instance.