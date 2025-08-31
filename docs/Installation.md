smtp4dev is available via the following distribution methods:
- [Docker/OCI container images](#how-to-run-smtp4dev-in-docker) - Linux and Windows images available.
- [.net SDK tool](#how-to-run-smtp4dev-as-a-dotnet-global-tool) - *recommended for Mac OS users* and available on any platform where .NET SDK is available.
- [Download from Github releases](#downloading-from-github-releases) - binaries available for Windows and Linux, and platform independent download avalable for any platform where .NET runtime is available.

See [Getting started](Getting-Started.md) for info on how to use it once you have installed and started smtp4dev.


## Downloading from github releases 

If you don't want to use the dotnet global tool or Docker (see below if you know what those mean), you can download a standalone (or .NET Core runtime dependent) release from github.

- Download [a release](https://github.com/rnwood/smtp4dev/releases) and unzip. To choose the right file, refer to the table below:

| Prefix | Description |
| -      | -           |
| Rnwood.Smtp4dev-win-x64 | Windows x64 (Intel 64 bit) binary standalone |
| Rnwood.Smtp4dev-noruntime | Architecture dependent version. Should run on any platform where the [.NET 8+ runtime](https://dotnet.microsoft.com/download/dotnet-core/current/runtime) is installed |
| Rnwood.Smtp4dev-linux-x64 | Linux x64 (Intel 64 bit) binary standalone |
| Rnwood.Smtp4dev-linux-musl-x64 | Linux x64 (Intel 64 bit) binary standalone for MUSL based distros (Alpine Linux) |
|Rnwood.Smtp4dev-linux-arm | Linux ARM (Intel 32 bit) binary standalone |
| Rnwood.Smtp4dev-win-arm | Windows ARM 32-bit binary standalone |

- On Linux `chmod +x` the `Rnwood.Smtp4dev` file to make it executable

- Edit ``appsettings.json`` and set the port number you want the SMTP server to listen on.

- Run `Rnwood.Smtp4dev` (`.exe` on Windows). (If you downloaded the ``noruntime`` version, you need the .NET Core 3.1 (.NET 6.0 if using smtp4dev version >= 3.2.x) runtime on your machine and you should execute ``dotnet Rnwood.Smtp4dev.dll`` to run it.)

## How to run smtp4dev as a dotnet global tool

If you're using the [.NET 8+ SDK](https://dotnet.microsoft.com/en-us/download/dotnet/latest) you can install smtp4dev as a global tool using the following command:
```
dotnet tool install -g Rnwood.Smtp4dev
```
The above will install the most recent non pre-release version.

If you want to use the pre-release development versions, add `--prerelease`

Then you can start smtp4dev by running
```
smtp4dev
```

## How to run smtp4dev in Docker
Docker images for both Windows and Linux are available. To run with the web interface on port 5000 and SMTP on port 2525:

```
docker run --rm -it -p 5000:80 -p 2525:25 rnwood/smtp4dev
```
Remove `--rm -it` if you want to leave smtp4dev running in the background, otherwise it will run until you hit CTRL+C.

The above will install the most recent non pre-release version. If you want to use the pre-release development versions use ``rnwood/smtp4dev:prerelease``. Both 'latest' and 'prerelease' are cross platform tag which will work on either Windows x64 or Linux x64. To see the full list of available tags [see the Docker hub page for smtp4dev](https://hub.docker.com/r/rnwood/smtp4dev/tags/).

The folder ``/smtp4dev`` will be used for the database and auto-generated TLS certificate. You can mount a directory outside of the container here for peristent storage.

[An example `docker-compose.yml` can be found here.](https://github.com/rnwood/smtp4dev/blob/master/docker-compose.yml)




## How to run smtp4dev as a Windows service

Download one the Windows standalone versions which is applicable for your OS/architecture.

### Install service in PowerShell

```
New-Service -Name Smtp4dev -BinaryPathName "{PathToExe} --service"
```

### Install service in Cmd or PowerShell

```
sc.exe create Smtp4dev binPath= "{PathToExe} --service"
```

## How to run smtp4dev in Windows IIS

Make sure you have installed the [ASP.NET Core Runtime - Windows Hosting Bundle](https://dotnet.microsoft.com/permalink/dotnetcore-current-windows-runtime-bundle-installer) so that IIS can host ASP.NET Core apps.

Also make sure the [IIS application pre-initialisation feature is installed](https://learn.microsoft.com/en-us/iis/get-started/whats-new-in-iis-8/iis-80-application-initialization#step-by-step-instructions).

Download a smtp4dev release and unzip. You must pick the `noruntime` version.

Create a site in IIS and set the root path to where you unzipped smtp4dev (do not point at `wwwroot` or `ClientApp`). You can also host in a virtual directory under another site, but don't forget to convert the directory to an application.

Grant permission to the IIS app pool to read the files. The principal name is `IIS APPPOOL\<name>` where `<name>` is the name of the app pool, which is the name of the site you created unless you changed something.

Edit the application pool advanced settings and ensure:
- '.NET CLR version' is set to 'No managed code'
- 'Start Mode' is set to 'AlwaysRunning'
- 'Idle Time-out (minutes)' is set to '0'
- Recycling > 'Regular Time Interval (minutes)' is set to '0'
- 'Preload enabled' is set to 'True'.

You can then access the site via the ports/hostname set in bindings that are set in IIS. If you see an error check the `Application` event log for details and you'll see any errors output from IIS.

