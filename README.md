# smtp4dev
smtp4dev - the mail server for development

A dummy SMTP server for Windows, Linux, Mac OS-X (and maybe elsewhere where .NET Core is available). This repository hosts the development of v3 which will have a re-written web UI so that it can be shared amongst members of a team (the most requested feature in v2).
For the stable smtp4dev version 2 (Windows only desktop app) please see [https://github.com/rnwood/smtp4dev/releases/tag/v2.0.10] and the v2.0 branch.

*If you find smtp4dev useful, please consider supporting further development by making a donation:*

<a href='https://www.paypal.me/rnwood'><img alt='Donate' src='https://www.paypalobjects.com/webstatic/en_US/btn/btn_donate_pp_142x27.png'/></a>

[![Build status](https://ci.appveyor.com/api/projects/status/tay9sajnfh4vy2x0/branch/master?svg=true)](https://ci.appveyor.com/project/rnwood/smtp4dev/branch/master)

## Screenshots

![Screenshot 1](screenshot1.png)
![Screenshot 2](screenshot2.png)

## How to run smtp4dev

The version hosted on this repo is in heavy development. **Grab the [stable(r) v2 version](https://github.com/rnwood/smtp4dev/releases/tag/v2.0.10) if you want something feature complete which you can just double click on and use.**

*The MacOS release is totally untested. Please contribute instructions on how to use and feedback on any issues*

- Download [a release](https://github.com/rnwood/smtp4dev/releases) and unzip.

- On Linux `chmod +x` the `Rnwood.Smtp4dev` file to make it executable

- Edit ``appsettings.json`` and set the port number you want the SMTP server to listen on.

- Run `Rnwood.Smtpdev` (`.exe` on Windows). (If you downloaded the ``noruntime`` version, you need the .NET Core 2.0 runtime on your machine and you should execute ``dotnet Rnwood.Smtpdev.dll`` to run it.)

- Open your browser at `http://localhost:5000`

- Now configure your apps which send mail, to use the SMTP server on the machine where SMTP4dev is running (``localhost`` if they are on the same machine), and using the port you selected (``25`` by default).


