# smtp4dev
smtp4dev - the mail server for development

A dummy SMTP server for Windows, Linux, Mac OS-X (and maybe elsewhere where .NET Core is available). 

This repository hosts the development of v3 which has a web UI so that it can be shared amongst members of a team, and be available cross platform.
This version is in development, but now approaching a stable state.

**If you're looking for the older v2 Windows GUI version. [Grab it here](https://github.com/rnwood/smtp4dev/releases/tag/v2.0.10).**

*If you find smtp4dev useful, please consider supporting further development by making a donation:*

<a href='https://www.paypal.me/rnwood'><img alt='Donate' src='https://www.paypalobjects.com/webstatic/en_US/btn/btn_donate_pp_142x27.png'/></a>

[![Build status](https://ci.appveyor.com/api/projects/status/tay9sajnfh4vy2x0/branch/master?svg=true)](https://ci.appveyor.com/project/rnwood/smtp4dev/branch/master)
[![Github Releases](https://img.shields.io/github/downloads/rnwood/smtp4dev/latest/total.svg)](https://github.com/rnwood/smtp4dev/releases) (+270k when prev hosted on Codeplex)

## Screenshots

![Screenshot 1](screenshot1.png)
![Screenshot 2](screenshot2.png)


## How to run smtp4dev by downloading from github releases 

*The MacOS release is totally untested. Please contribute instructions on how to use and feedback on any issues*

- Download [a release](https://github.com/rnwood/smtp4dev/releases) and unzip.

- On Linux `chmod +x` the `Rnwood.Smtp4dev` file to make it executable

- Edit ``appsettings.json`` and set the port number you want the SMTP server to listen on.

- Run `Rnwood.Smtp4dev` (`.exe` on Windows). (If you downloaded the ``noruntime`` version, you need the .NET Core 2.0 runtime on your machine and you should execute ``dotnet Rnwood.Smtpdev.dll`` to run it.)

- Open your browser at `http://localhost:5000` (to run the web server on a different port or make it listen on interfaces other than loopback, add the command line arg `--server.urls "http://0.0.0.0:5001/"` when starting the executable.

- Now configure your apps which send mail, to use the SMTP server on the machine where SMTP4dev is running (``localhost`` if they are on the same machine), and using the port you selected (``25`` by default).

## How to run smtp4dev as a dotnet global tool

If you're using the .NET Core SDK 2.2 or greater, you can install smtp4dev as a global tool using the following command:
```
dotnet tool install -g Rnwood.Smtp4dev --version "3.1.0-*"
```
Then you can start smtp4dev by running
```
smtp4dev
```


## How to run smtp4dev in Docker
Docker images for both Windows and Linux are available. To run with the web interface on port 3000 and SMTP on port 2525:

**Linux or Linux Containers on Windows/Other-OSs:**
```
docker run -p 3000:80 -p 2525:25 rnwood/smtp4dev:linux-amd64-v3
```
**Windows:**
```
docker run -p 3000:80 -p 2525:25 rnwood/smtp4dev:windows-amd64-v3
```
Sorry no unified cross platform image tag available yet. To see the full list of available tags [see the Docker hub page for smtp4dev](https://hub.docker.com/r/rnwood/smtp4dev/tags/).

## How to run smtp4dev as a service (Windows only)

A service in Windows can be installed using New-Service in PowerShell, or sc in both command line or PowerShell. If you use sc in PowerShell, it must be run as sc.exe. sc is an alias for Set-Content.

### Install service in PowerShell

```
New-Service -Name Smtp4dev -BinaryPathName "{PathToExe} --service"
```

### Install service in Cmd or PowerShell

```
sc.exe create Smtp4dev binPath= "{PathToExe} --service"
```


### Configuration
#### Changing how many messages or sessions are kept
smtp4dev keeps the latest 100 messages and 100 sessions by default.
The ``--messagestokeep X`` and ``--sessionstokeep X`` command line options can override this.

#### Changing where the database file is saved
By default smtp4dev will create a Sqlite DB named ``database.db`` in application root direction when it runs.

To change the path of this file use the ``--db "/path/to/file.db"`` command line option or edit ``ServerOptions\Database`` in ``appsettings.json``

To use an in memory DB use an empty string (e.g. ``--db=""``)). All session and messages will be lost when the process exits. (Docker: The ``=`` in the example is important as Docker will eat the empty quotes if ommitted.)

#### Changing the SMTP port
smtp4dev listens on 0.0.0.0 (all interfaces) port 25 by default. To change this either edit `ServerOptions\Port` in the ``appsettings.json`` before startup or add the ``--smtpport`` command line options (e.g. ``--smtpport 2525``).
