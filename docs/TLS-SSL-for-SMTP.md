## SMTP TLS Mode

By default TLS/SSL for SMTP is disabled.

It can be enabled using the `--tlsmode` command line argument or `TlsMode` settings property:

* `StartTls` - Connection starts off without TLS and then switches to TLS when client requests it with the `STARTTLS` command.
* `ImplicitTls` - Connection starts off immediately with TLS. Sometimes called SMTPS.
* `None` - TLS/SSL is off and will not be available.

This must match how your SMTP clients are configured. If this is mismatched you will see errors from you client upon connection.

## Certificate

### Auto Generated
By default once enabled, a self signed certificate will be generated and used using the hostname that is configured in the `--hostname`/`HostName`.

The certificate will be re-used unless the hostname changes or expiry - 10 years.

### Manually Provided

You can supply your own certificate using the `--tlscertificate` `--tlscertificateprivatekey` and `--tlscertificatepassword` command line options and corresponding settings. See the comments in settings or command line `--help` for more details.

The certificate provided must contain the private key. Either as part of a bundle for `tlscertificate` or separately via `tlscertificateprivatekey`. 

## Client Validation and Trust

Clients often  validate that they trust the certificate presented by the server:
* Make sure when you enter the SMTP server hostname into your client config, it matches that you have configured smtp4dev with. 
  Clients will reject the certifiate if there is a mismatch, for instance if smtp4dev is configured with `mymachine` and the client connects using `localhost`.
* Configure your client to trust the certificate. The path of auto generated certificate is printed during start up.
  On Windows, import the certificate into the `Trusted Root Certification Authority` store. Check the documentation for the client/device to check how to do this. Alternatively, you may be able to disable certificate trust validation.
