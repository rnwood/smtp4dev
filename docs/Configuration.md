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