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

### Important Notes

1. **No Message Duplication**: Due to "first match wins" logic, each recipient goes to exactly one mailbox

2. **Case Sensitivity**: All pattern matching is case-insensitive

3. **Multiple Recipients**: When an email has multiple recipients, each recipient is processed independently and may go to different mailboxes

4. **Authenticated Users**: If `DeliverMessagesToUsersDefaultMailbox` is enabled and users are authenticated, messages go to the user's configured default mailbox instead of following recipient patterns

5. **Performance**: Wildcard patterns are generally faster than regular expressions for simple matching

### Troubleshooting

**Messages not appearing in expected mailbox**: Verify the order of mailboxes - earlier mailboxes take precedence over later ones due to "first match wins" logic.

**Regex not working**: Ensure the pattern is surrounded by forward slashes (`/pattern/`) and test the regex with an online regex tester.

**Want messages in multiple mailboxes**: This is not supported due to "first match wins" logic. Consider using a single mailbox with multiple recipient patterns instead.