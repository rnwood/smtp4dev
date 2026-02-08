smtp4dev configuration can be performed via the `appsettings.json` file, environment variables and command-line arguments. 

## In the User Interface

**✨Many of the basic settings can be edited in the UI** 

To configure, simply start up smtp4dev and click the settings icon at the top-right of the screen.

When saved, these are written to the settings file at `{AppData}/smtp4dev/appsettings.json`

## Configuration Files

You can find the default configuration file at `<installlocation>/appsettings.json`. This file is included in every release and will be overwritten when you update. To avoid this, create a 'user' configuration file at `{AppData}/smtp4dev/appsettings.json` and make your customisations there.

`{AppData}` is platform dependent but normally:
- Windows - environment variable: `APPDATA`
- Linux & Mac - environment variable: `XDG_CONFIG_HOME`

The search path of these files is printed when smtp4dev starts up. So the easiest way to find them is to look there:

```
smtp4dev version 3.3.6-ci20240419116+60aff5ea69aa19c6fb9afa8573fd5f77ab40de3a
https://github.com/rnwood/smtp4dev
.NET Core runtime version: .NET 8.0.4

 > For help use argument --help

Install location: C:\Users\rob
DataDir: C:\Users\rob\AppData\Roaming\smtp4dev
Default settings file: C:\Users\rob\.dotnet\tools\.store\rnwood.smtp4dev\3.3.6-ci20240419116\rnwood.smtp4dev\3.3.6-ci20240419116\tools\net8.0\any\appsettings.json
User settings file: C:\Users\rob\AppData\Roaming\smtp4dev\appsettings.json
```

Version 3.1.2 onwards will automatically reload and apply any edits to the configuration file without restarting. 

[List of app settings](https://github.com/rnwood/smtp4dev/blob/master/Rnwood.Smtp4dev/appsettings.json)

Note that this will vary by version of smtp4dev, so for best results, open the settings file you have!

## Environment Variables

All the values from `appsettings.json` can be overridden by environment variables.

Set environmment variables in the format: `ServerOptions__HostName`.

For arrays, use the format `ServerOptions__Users__0__User` where `Users` is the property holding the array, `0` is the index of the item and `User` is one of the properties of that item.

## Command Line Options

To see the command line options, run `Rnwood.Smtp4dev(.exe)` or `Rnwood.Smtp4dev.Desktop(.exe)` with `--help`.

## Mailbox Configuration

smtp4dev supports multiple virtual mailboxes to organize incoming messages. This is particularly useful for testing applications that send different types of emails.

### How Mailbox Routing Works

1. **Processing Order**: Mailboxes are processed in the order they appear in the configuration
2. **First Match Wins**: Each recipient is matched against mailbox recipient patterns, and the first match determines the destination
3. **Default Mailbox**: A default mailbox with `Recipients="*"` (catch-all) is automatically added as the last mailbox
4. **Single Delivery**: Each message is delivered only once to each mailbox
5. **No Duplication**: Due to "first match wins" logic, messages go to exactly one mailbox per recipient

### Recipient Pattern Syntax

**Wildcards** (recommended for simple patterns):
- `*@example.com` - Any user at example.com
- `user@*` - Specific user at any domain  
- `*sales*@*` - Any address containing "sales"

**Regular Expressions** (for complex patterns, surround with `/`):
- `/.*@(sales|marketing)\.com$/` - Addresses ending with @sales.com or @marketing.com
- `/^(admin|root)@.*$/` - Addresses starting with admin@ or root@

### Common Mailbox Scenarios

#### Example: Department-Based Routing

```json
{
  "ServerOptions": {
    "Mailboxes": [
      {
        "Name": "Sales",
        "Recipients": "*@sales.example.com, sales@*"
      },
      {
        "Name": "Support", 
        "Recipients": "support@*, help@*"
      }
    ]
  }
}
```

With this configuration:
- `john@sales.example.com` → **Sales** mailbox only
- `support@company.com` → **Support** mailbox only  
- `admin@company.com` → **Default** mailbox only (no custom match)

#### Example: Spam Filtering

```json
{
  "ServerOptions": {
    "Mailboxes": [
      {
        "Name": "Spam",
        "Recipients": "*spam*@*, *test*@*, *junk*@*"
      }
    ]
  }
}
```

With this configuration:
- `spam-user@example.com` → **Spam** mailbox only
- `test@company.com` → **Spam** mailbox only
- `regular@company.com` → **Default** mailbox only

### Advanced Configuration

#### Custom Default Mailbox (Optional)

If you want to customize the default mailbox behavior, you can explicitly configure it:

```json
{
  "ServerOptions": {
    "Mailboxes": [
      {
        "Name": "Alerts",
        "Recipients": "*alert*@*, *notification*@*"
      },
      {
        "Name": "Default",
        "Recipients": "*"
      }
    ]
  }
}
```

**Note**: Explicitly configuring the default mailbox is typically unnecessary since it's automatically added with the same pattern (`*`).

### Header-Based Message Routing

In addition to recipient-based routing, smtp4dev supports routing messages based on email headers. This is useful when:
- Multiple applications share the same SMTP credentials but need separate mailboxes
- You want to filter by spam scores, antivirus headers, or custom application headers
- Messages need to be routed based on metadata that isn't in the recipient address

#### Header Filter Configuration

Add `HeaderFilters` to any mailbox configuration:

```json
{
  "ServerOptions": {
    "Mailboxes": [
      {
        "Name": "SRS",
        "Recipients": "*",
        "HeaderFilters": [
          {
            "Header": "X-Application",
            "Pattern": "srs"
          }
        ]
      }
    ]
  }
}
```

**How it works:**
1. **All filters must match**: If multiple `HeaderFilters` are specified, ALL must match for the message to be routed to that mailbox
2. **Header filters checked first**: Before checking recipient patterns, header filters are evaluated
3. **First match wins**: Once a mailbox matches (headers + recipients), no other mailboxes are checked

#### Header Pattern Types

**Exact/Wildcard Match (Glob):**
```json
{ "Header": "X-Application", "Pattern": "srs" }
{ "Header": "X-Mailer", "Pattern": "srs-*" }
```

**Regular Expression (case-insensitive, surround with `/`):**
```json
{ "Header": "X-Priority", "Pattern": "/^(high|urgent)$/" }
{ "Header": "X-Spam-Score", "Pattern": "/^[0-4]\\./" }
```

**Header Existence Check (any value):**
```json
{ "Header": "X-Antivirus", "Pattern": ".*" }
```

#### Example Scenarios

**Route by Application Identifier:**
```json
{
  "Mailboxes": [
    {
      "Name": "SRS",
      "Recipients": "*",
      "HeaderFilters": [
        { "Header": "X-Application", "Pattern": "srs" }
      ]
    },
    {
      "Name": "USOSapi",
      "Recipients": "*"
    }
  ]
}
```
Messages with `X-Application: srs` go to "SRS" mailbox, all others go to "USOSapi" mailbox.

**Route Antivirus-Scanned Messages:**
```json
{
  "Mailboxes": [
    {
      "Name": "Scanned",
      "Recipients": "*",
      "HeaderFilters": [
        { "Header": "X-Antivirus", "Pattern": ".*" }
      ]
    }
  ]
}
```
Any message with an `X-Antivirus` header (regardless of value) goes to "Scanned" mailbox.

**Combine Multiple Header Filters:**
```json
{
  "Mailboxes": [
    {
      "Name": "Critical-Sales",
      "Recipients": "*@sales.com",
      "HeaderFilters": [
        { "Header": "X-Priority", "Pattern": "/^(high|urgent)$/" },
        { "Header": "X-Department", "Pattern": "sales" }
      ]
    }
  ]
}
```
Only messages to `@sales.com` with BOTH `X-Priority: high` (or `urgent`) AND `X-Department: sales` headers.

### Important Notes

1. **No Message Duplication**: Due to "first match wins" logic, each recipient goes to exactly one mailbox

2. **Case Sensitivity**: All pattern matching (recipients and headers) is case-insensitive

3. **Multiple Recipients**: When an email has multiple recipients, each recipient is processed independently and may go to different mailboxes

4. **Authenticated Users**: If `DeliverMessagesToUsersDefaultMailbox` is enabled and users are authenticated, messages go to the user's configured default mailbox instead of following recipient patterns

5. **Performance**: Wildcard patterns are generally faster than regular expressions for simple matching

6. **Header Filter Performance**: Headers are only parsed when at least one mailbox has `HeaderFilters` configured

### Troubleshooting

**Messages not appearing in expected mailbox**: Verify the order of mailboxes - earlier mailboxes take precedence over later ones due to "first match wins" logic.

**Regex not working**: Ensure the pattern is surrounded by forward slashes (`/pattern/`) and test the regex with an online regex tester.

## Deliver to Stdout Feature

smtp4dev can output raw message content to stdout for automated processing or testing scenarios. This feature is useful for CI/CD pipelines, automated testing, or integrating with other tools.

### Configuration Options

#### DeliverToStdout

Specifies which mailboxes should have their messages output to stdout:

- **`*`** - Deliver all messages from all mailboxes to stdout
- **`mailbox1,mailbox2`** - Deliver only messages from specified mailboxes (comma-separated)
- **Empty string** (default) - Disable stdout delivery

**Command Line**: `--delivertostdout="*"` or `--delivertostdout="Sales,Support"`

**Configuration File**:
```json
{
  "ServerOptions": {
    "DeliverToStdout": "*"
  }
}
```

#### ExitAfterMessages

Automatically exit the application after delivering a specified number of messages to stdout. Useful for automated testing scenarios.

**Command Line**: `--exitafter=5`

**Configuration File**:
```json
{
  "ServerOptions": {
    "ExitAfterMessages": 5
  }
}
```

### Message Format

Messages delivered to stdout are wrapped with delimiters to separate multiple messages:

```
--- BEGIN SMTP4DEV MESSAGE ---
<raw message content>
--- END SMTP4DEV MESSAGE ---
```

### Logging Behavior

When using deliver to stdout, all diagnostic and application logs are automatically redirected to stderr, ensuring stdout contains only message content.

### Example Usage

**Capture all messages and exit after 10:**
```bash
smtp4dev --delivertostdout="*" --exitafter=10 --smtpport=2525 > messages.txt 2> logs.txt
```

**Capture only Sales mailbox messages:**
```bash
smtp4dev --mailbox="Sales=*sales*@*" --delivertostdout="Sales" --smtpport=2525 > sales-messages.txt
```

**Use in CI/CD pipeline:**
```bash
# Start smtp4dev in background, send test emails, capture output
smtp4dev --delivertostdout="*" --exitafter=3 --smtpport=2525 --imapport=0 --pop3port=0 > captured-emails.txt 2>&1 &
# ... send test emails ...
# Process captured-emails.txt
```


**Want messages in multiple mailboxes**: This is not supported due to "first match wins" logic. Consider using a single mailbox with multiple recipient patterns instead.

## OAuth2/XOAUTH2 Authentication

smtp4dev supports OAuth2/XOAUTH2 authentication for SMTP connections, allowing you to validate access tokens against an Identity Provider (IDP) such as Azure AD, Google, Okta, or any OpenID Connect compatible provider.

### How OAuth2 Authentication Works

When a client connects using XOAUTH2 authentication:

1. **With `SmtpAllowAnyCredentials=true`**: Any OAuth2 token is accepted without validation (default behavior, suitable for development)
2. **With `SmtpAllowAnyCredentials=false`** and OAuth2Authority configured:
   - The access token is validated against the configured IDP
   - Token signature, expiration, issuer, and audience are verified
   - The subject claim from the token must match the provided username (case-insensitive)
   - The username must exist in the configured Users list
   - Authentication fails if validation fails, the subject doesn't match, or the user is not configured

### Configuration

To enable OAuth2 token validation, configure the following settings:

#### OAuth2Authority (Required for validation)

The OpenID Connect authority URL for your IDP. This URL should point to the base URL where the OpenID Connect discovery document can be found.

**Examples:**
- Azure AD (multi-tenant): `https://login.microsoftonline.com/common/v2.0`
- Azure AD (single tenant): `https://login.microsoftonline.com/{tenant-id}/v2.0`
- Google: `https://accounts.google.com`
- Okta: `https://{your-domain}.okta.com/oauth2/default`

**Command Line**: `--oauth2authority="https://login.microsoftonline.com/common/v2.0"`

**Configuration File**:
```json
{
  "ServerOptions": {
    "OAuth2Authority": "https://login.microsoftonline.com/common/v2.0"
  }
}
```

**Environment Variable**: `ServerOptions__OAuth2Authority`

#### OAuth2Audience (Optional but recommended)

The expected audience value for tokens. If specified, tokens must be issued for this audience.

**Command Line**: `--oauth2audience="your-app-id"`

**Configuration File**:
```json
{
  "ServerOptions": {
    "OAuth2Audience": "api://your-application-id"
  }
}
```

**Environment Variable**: `ServerOptions__OAuth2Audience`

#### OAuth2Issuer (Optional)

The expected issuer value for tokens. If not specified, the issuer is validated using the authority's discovery document.

**Command Line**: `--oauth2issuer="https://login.microsoftonline.com/{tenant-id}/v2.0"`

**Configuration File**:
```json
{
  "ServerOptions": {
    "OAuth2Issuer": "https://login.microsoftonline.com/{tenant-id}/v2.0"
  }
}
```

**Environment Variable**: `ServerOptions__OAuth2Issuer`

#### Users (Required when SmtpAllowAnyCredentials=false)

When OAuth2 authentication is used with `SmtpAllowAnyCredentials=false`, you must configure the allowed users. The username in the Users list must match the subject claim from the OAuth2 token (case-insensitive).

**Command Line**: `--user="john@example.com=password"`

Note: The password field is required for the Users configuration but is not used for OAuth2 authentication. You can set it to any value.

**Configuration File**:
```json
{
  "ServerOptions": {
    "Users": [
      {
        "Username": "john@example.com",
        "Password": "not-used-for-oauth2"
      },
      {
        "Username": "jane@example.com",
        "Password": "not-used-for-oauth2"
      }
    ]
  }
}
```

**Environment Variable**: `ServerOptions__Users__0__Username`, `ServerOptions__Users__0__Password`

### Complete Example

**Azure AD Configuration:**
```json
{
  "ServerOptions": {
    "AuthenticationRequired": true,
    "SmtpAllowAnyCredentials": false,
    "OAuth2Authority": "https://login.microsoftonline.com/common/v2.0",
    "OAuth2Audience": "api://your-application-id",
    "SmtpEnabledAuthTypesWhenNotSecureConnection": "XOAUTH2",
    "SmtpEnabledAuthTypesWhenSecureConnection": "XOAUTH2",
    "Users": [
      {
        "Username": "john@example.com",
        "Password": "not-used-for-oauth2"
      }
    ]
  }
}
```

**Google OAuth2 Configuration:**
```json
{
  "ServerOptions": {
    "AuthenticationRequired": true,
    "SmtpAllowAnyCredentials": false,
    "OAuth2Authority": "https://accounts.google.com",
    "OAuth2Audience": "your-google-client-id.apps.googleusercontent.com",
    "Users": [
      {
        "Username": "john@example.com",
        "Password": "not-used-for-oauth2"
      }
    ]
  }
}
```

### Subject Claim Mapping

The token validator looks for the username/email in the following claims (in order):
1. `sub` - Subject identifier
2. `email` - Email address
3. `preferred_username` - Preferred username
4. `upn` - User Principal Name

The value from the first found claim must match the username provided in the XOAUTH2 authentication data.

### Troubleshooting

**Authentication fails with "OAuth2Authority not configured":**
- Ensure `OAuth2Authority` is set when `SmtpAllowAnyCredentials` is false
- Verify the authority URL is correct and accessible

**Authentication fails with "Token validation error":**
- Check that the token hasn't expired
- Verify the token was issued by the configured authority
- Ensure the audience claim matches `OAuth2Audience` if configured
- Check server logs for detailed error messages

**Authentication fails with "subject mismatch":**
- The subject claim in the token must match the username provided in the XOAUTH2 authentication
- Both values are compared case-insensitively
- Check which claim is being used (sub, email, preferred_username, or upn)

**Authentication fails with "username not in configured users list":**
- When `SmtpAllowAnyCredentials=false`, the username must be in the configured Users list
- Add the user to the Users configuration with any password (password is not used for OAuth2)
- Ensure the username matches the subject claim from the token (case-insensitive)

### Development vs Production

**Development Mode** (default):
```json
{
  "ServerOptions": {
    "SmtpAllowAnyCredentials": true
  }
}
```
- Any credentials are accepted
- OAuth2 tokens are not validated
- Suitable for local development and testing

**Production Mode**:
```json
{
  "ServerOptions": {
    "AuthenticationRequired": true,
    "SmtpAllowAnyCredentials": false,
    "OAuth2Authority": "https://your-idp.com",
    "OAuth2Audience": "your-app-id",
    "Users": [
      {
        "Username": "allowed-user@example.com",
        "Password": "not-used-for-oauth2"
      }
    ]
  }
}
```
- Credentials are validated
- OAuth2 tokens are validated against the IDP
- Subject must match username
- Username must be in configured Users list

## Working Example

See the [OAuth2/XOAUTH2 with JHipster Registry example](../examples/oauth2-jhipster/) for a complete working demonstration of OAuth2 authentication using Docker.