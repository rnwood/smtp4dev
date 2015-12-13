# smtp4dev
smtp4dev - the mail server for development

A dummy SMTP server for Windows. Sits in the system tray and does not deliver the received messages. The received messages can be quickly viewed, saved and the source/structure inspected. Useful for testing/debugging software that generates email.

*If you find smtp4dev useful, please consider supporting further development by making a donation:*

<a href='https://www.paypal.me/rnwood'><img alt='Donate' src='https://www.paypalobjects.com/webstatic/en_US/btn/btn_donate_pp_142x27.png'/></a>

## Configuring software to send to smtp4dev
smtp4dev should work with most software that sends messages using the SMTP protocol.
Configure your software to send mail to localhost. If the software requires an SSL connection then this must be enabled in the options dialog and you need to supply a cerficate.
