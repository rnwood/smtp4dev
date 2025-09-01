1. **Install smtp4dev** [by following the instruction here](Installation.md)
  
2. **Run `Rnwood.Smtp4dev(.exe)`** (or if using the .NET tool or docker, it's different - see the link above)

3. **Open the web interface in your browser**. 

   Smtp4dev prints out the address of the web interface to the console as it starts up. It's http://localhost:5000 by default.

   ```Now listening on: http://localhost:5000```

5. **Check that the SMTP server has started successfully**.

   This is shown at the top right of the screen and should say `SMTP server is listning on port X`.

   If it instead says something else, this usually indicates that another program is using this port, or you do not have permission to use this port. You can pick a different port in the settings dialog; click the settings button at top right of screen.

4. **Configure your mail sending apps**. 

   [Any programs that you want to send mail to smtp4dev need to be configured](Configuring-Clients.md) so that they send mail via SMTP to the host/address and port number where smtp4dev is running. **Use the port number you saw/chose above**.

5. **Any messages received by smtp4dev will be displayed in the list.**