Any programs that you want to send mail to smtp4dev need to be configured so that they send mail via SMTP to the host/address and port number where smtp4dev is running.


Look for the following options in your program/platform
|Option|Value |
|-|-|
|SMTP hostname |`localhost` if client is on same machine as smtp4dev<br/>or the DNS name of the machine where smtp4dev is running e.g. `mymachine.mydomain`.|
| SMTP port number | `25` (or the port you have chosen for SMTP server in smtp4dev) | 
| SSL/TLS, Secure connection, Encryption | `Off` or `Not required` etc<br><br>TLS is supported by SMTP4DEV if your client insists, but must be turned on. See configuration section. |
| Authentication | `Off`.<br><br>See below.
| Username/Password | Empty<br><br>Authentication is not required, but by default will be accepted with any credentials if your client insists on this (can be turned off using the `Allow any credentials` (SMTP) option). Alternatively you can turn on the `Require Authentication` (SMTP) option in the smtp4dev options and set up a user in the `Users` section, then use the username password you set up here.

This is valid for the default configuration for smtp4dev. [See configuration information](Configuration.md) for information on how to check and change this including how to enable SSL/TLS.
