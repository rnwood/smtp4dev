param($count=1)


$ErrorActionPreference="Stop"
write-host "Here"

1..$count | %{ 

[System.Net.ServicePointManager]::ServerCertificateValidationCallback = {
    param($source, $cert, $chain, $errors)
    write-host "TLS cert errors: $errors"
    return $true
}
    write-host $i

    Send-MailMessage -credential (get-credential) -verbose -Attachments ./smtp4dev.sln -To to@bar.com -Cc cc@bar.com -bcc bcc@bar.com -From from@from.com -Subject "Message $i" -SmtpServer localhost -BodyAsHtml -Body @"
<!doctype html><html lang="en" xmlns="http://www.w3.org/1999/xhtml" xmlns:v="urn:schemas-microsoft-com:vml" xmlns:o="urn:schemas-microsoft-com:office:office"><head><title>test appt</title><!--[if !mso]><!--><meta http-equiv="X-UA-Compatible" content="IE=edge"><!--<![endif]--><meta http-equiv="Content-Type" content="text/html; charset=UTF-8"><meta name="viewport" content="width=device-width, initial-scale=1">
<style type="text/css">
#outlook a { padding: 0; }
body { margin: 0; padding: 0; -webkit-text-size-adjust: 100%; -ms-text-size-adjust: 100%; }
table, td { border-collapse: collapse; mso-table-lspace: 0pt; mso-table-rspace: 0pt; }
img { border: 0; height: auto; line-height: 100%; outline: none; text-decoration: none; -ms-interpolation-mode: bicubic; }
p { display: block; margin: 13px 0; }
</style>
<!--[if mso]>
<noscript>
<xml>
<o:OfficeDocumentSettings>
  <o:AllowPNG/>
  <o:PixelsPerInch>96</o:PixelsPerInch>
</o:OfficeDocumentSettings>
</xml>
</noscript>
<![endif]-->
<!--[if lte mso 11]>
<style type="text/css">
.mj-outlook-group-fix { width:100% !important; }
</style>
<![endif]-->
<style type="text/css">@media only screen and (min-width:480px) { .mj-column-per-100 { width:100% !important; max-width:100%; } .mj-column-per-33-333332 { width:33.333332% !important; max-width:33.333332%; }  }</style><style media="screen and (min-width:480px)">.moz-text-html .mj-column-per-100 { width:100% !important; max-width:100%; } .moz-text-html .mj-column-per-33-333332 { width:33.333332% !important; max-width:33.333332%; } </style><style type="text/css">h1,h2,h3,h4 {
        display: block;
        font-style: normal;
        font-weight: bold;
        line-height: 140%;
        margin: 0;
        text-align: left;
      }
      h1 {
        color: #262626;
        font-weight: bold;
        font-size: 1.3077em;
      }
      h2 {
        color: #808080;
        font-weight: bold;
        font-size: 1em;
      }
      a {
        color: #0D74E3;
      }
      .respond-button a {
        color: #000000;
      }

      .status-dot {
        height: 10px;
        width: 10px;
        border-radius: 50%;
        display: inline-block;
        margin-right: 6px;
        vertical-align: middle;
      }
      .accept-dot { background-color: #28cd41; }
      .decline-dot { background-color: #ff3b30; }
      .maybe-dot { background-color: #ff9500; }

      /* Dark theme styles */

      @media (prefers-color-scheme: dark) {
        .default-color,
        .default-color > div {
          color: #d9d9d9 !important;
        }
        .default-bgcolor,
        .default-bgcolor > table {
          background: #000000 !important;
          background-color: #000000 !important;
        }

        .border-color,
        .border-color > table {
          background: #191919 !important;
          background-color: #191919 !important;
        }

        .blockheader,
        .blockheader > table {
          background: #121212 !important;
          background-color: #121212 !important;
        }

        .blockheader h1 {
          color:#d9d9d9 !important;
        }
        .blockheader .datetime,
        .blockheader .datetime > div {
          color:#a6a6a6 !important;
        }

        .respond-button td,
        .respond-button td a {
          background: #000000 !important;
          background-color: #000000 !important;
          color: #ffffff !important;
        }
        .respond-button td {
          border-color: #272727 !important;
        }
      }</style><meta name="color-scheme" content="light dark" /><meta name="supported-color-schemes" content="light dark" /></head><body style="word-spacing:normal;background-color:#ffffff;"><div class="default-bgcolor" lang="en" style="background-color:#ffffff;"><!--[if mso | IE]><table border="0" cellpadding="0" cellspacing="0" role="presentation" align="center" width="600" style="width:600px;"><tr><td style="line-height:0px;font-size:0px;mso-line-height-rule:exactly;"><![endif]--><div style="margin:0px auto;max-width:600px;"><table border="0" cellpadding="0" cellspacing="0" role="presentation" align="center" style="width:100%;"><tbody><tr><td style="direction:ltr;font-size:0px;padding:30px;text-align:center;"><!--[if mso | IE]><table border="0" cellpadding="0" cellspacing="0" role="presentation"><tr><td style="vertical-align:top;width:600px;"><![endif]--><div class="mj-outlook-group-fix mj-column-per-100" style="font-size:0px;text-align:left;direction:ltr;display:inline-block;vertical-align:top;width:100%;"><table border="0" cellpadding="0" cellspacing="0" role="presentation" width="100%"><tbody><tr><td style="vertical-align:top;padding:0;"><table border="0" cellpadding="0" cellspacing="0" role="presentation" width="100%"><tbody><tr><td align="left" class="default-color" style="font-size:0px;padding:0;word-break:break-word;"><div style="font-family:-apple-system, BlinkMacSystemFont, 'Segoe UI', 'Roboto', Helvetica, Arial, ui-sans-serif, sans-serif;font-size:13px;line-height:1.5;text-align:left;color:#262626;">

            name has invited you to a meeting.

        </div></td></tr></tbody></table></td></tr></tbody></table></div><!--[if mso | IE]></td></tr></table><![endif]--></td></tr></tbody></table></div><!--[if mso | IE]></td></tr></table><![endif]--><!--[if mso | IE]><table border="0" cellpadding="0" cellspacing="0" role="presentation" bgcolor="#e5e5e5" align="center" width="600" class="border-color-outlook" style="width:600px;"><tr><td style="line-height:0px;font-size:0px;mso-line-height-rule:exactly;"><![endif]--><div class="border-color" style="background:#e5e5e5;background-color:#e5e5e5;margin:0px auto;border-radius:8px;max-width:600px;"><table border="0" cellpadding="0" cellspacing="0" role="presentation" align="center" style="background:#e5e5e5;background-color:#e5e5e5;width:100%;border-radius:8px;"><tbody><tr><td style="direction:ltr;font-size:0px;padding:1px;text-align:center;"><!--[if mso | IE]><table border="0" cellpadding="0" cellspacing="0" role="presentation"><tr><td width="600px" class="blockheader-outlook"><![endif]--><!--[if mso | IE]><table border="0" cellpadding="0" cellspacing="0" role="presentation" bgcolor="#f7f7f7" align="center" width="598" class="blockheader-outlook" style="width:598px;"><tr><td style="line-height:0px;font-size:0px;mso-line-height-rule:exactly;"><![endif]--><div class="blockheader" style="background:#f7f7f7;background-color:#f7f7f7;margin:0px auto;border-radius:8px 8px 0 0;max-width:598px;"><table border="0" cellpadding="0" cellspacing="0" role="presentation" align="center" style="background:#f7f7f7;background-color:#f7f7f7;width:100%;border-radius:8px 8px 0 0;"><tbody><tr><td style="direction:ltr;font-size:0px;padding:30px;text-align:center;"><!--[if mso | IE]><table border="0" cellpadding="0" cellspacing="0" role="presentation"><tr><td style="vertical-align:top;width:598px;"><![endif]--><div class="mj-outlook-group-fix mj-column-per-100" style="font-size:0px;text-align:left;direction:ltr;display:inline-block;vertical-align:top;width:100%;"><table border="0" cellpadding="0" cellspacing="0" role="presentation" width="100%"><tbody><tr><td style="vertical-align:top;padding:0;"><table border="0" cellpadding="0" cellspacing="0" role="presentation" width="100%"><tbody><tr><td align="left" style="font-size:0px;padding:0;word-break:break-word;"><div style="font-family:-apple-system, BlinkMacSystemFont, 'Segoe UI', 'Roboto', Helvetica, Arial, ui-sans-serif, sans-serif;font-size:13px;line-height:1.5;text-align:left;color:#262626;"><h1>test appt</h1></div></td></tr><tr><td align="left" class="datetime" style="font-size:0px;padding:15px 0 0 0;word-break:break-word;"><div style="font-family:-apple-system, BlinkMacSystemFont, 'Segoe UI', 'Roboto', Helvetica, Arial, ui-sans-serif, sans-serif;font-size:15px;line-height:1.5;text-align:left;color:#595959;">
            Friday, June 27, 2025<br />1:00 PM – 2:00 PM (EDT)
          </div></td></tr></tbody></table></td></tr></tbody></table></div><!--[if mso | IE]></td></tr></table><![endif]--></td></tr></tbody></table></div><!--[if mso | IE]></td></tr></table><![endif]--><!--[if mso | IE]></td></tr><tr><td width="600px" class="blockheader-outlook"><![endif]--><!--[if mso | IE]><table border="0" cellpadding="0" cellspacing="0" role="presentation" bgcolor="#f7f7f7" align="center" width="598" class="blockheader-outlook" style="width:598px;"><tr><td style="line-height:0px;font-size:0px;mso-line-height-rule:exactly;"><![endif]--><div class="blockheader" style="background:#f7f7f7;background-color:#f7f7f7;margin:0px auto;max-width:598px;"><table border="0" cellpadding="0" cellspacing="0" role="presentation" align="center" style="background:#f7f7f7;background-color:#f7f7f7;width:100%;"><tbody><tr><td style="direction:ltr;font-size:0px;padding:0 0 30px 0;text-align:center;"><!--[if mso | IE]><table border="0" cellpadding="0" cellspacing="0" role="presentation"><tr><td style="vertical-align:top;width:199.33331px;"><![endif]--><div class="mj-outlook-group-fix mj-column-per-33-333332" style="font-size:0px;text-align:left;direction:ltr;display:inline-block;vertical-align:top;width:100%;"><table border="0" cellpadding="0" cellspacing="0" role="presentation" width="100%"><tbody><tr><td style="vertical-align:top;padding:0;"><table border="0" cellpadding="0" cellspacing="0" role="presentation" width="100%"><tbody><tr><td align="center" vertical-align="middle" class="respond-button" style="font-size:0px;padding:5px;word-break:break-word;"><table border="0" cellpadding="0" cellspacing="0" role="presentation" style="border-collapse:separate;width:150px;line-height:100%;"><tbody><tr><td align="center" bgcolor="#ffffff" role="presentation" valign="middle" style="border:1px solid #d9d9d9;border-radius:6px;cursor:auto;mso-padding-alt:10px 25px;background:#ffffff;"><a href="#" target="_blank" style="display:inline-block;width:100px;background:#ffffff;color:#000000;font-family:-apple-system, BlinkMacSystemFont, 'Segoe UI', 'Roboto', Helvetica, Arial, ui-sans-serif, sans-serif;font-size:15px;font-weight:600;line-height:120%;margin:0;text-decoration:none;text-transform:none;padding:10px 25px;mso-padding-alt:0px;border-radius:6px;"><span class="status-dot accept-dot"></span>Accept
          </a></td></tr></tbody></table></td></tr></tbody></table></td></tr></tbody></table></div><!--[if mso | IE]></td><td style="vertical-align:top;width:199.33331px;"><![endif]--><div class="mj-outlook-group-fix mj-column-per-33-333332" style="font-size:0px;text-align:left;direction:ltr;display:inline-block;vertical-align:top;width:100%;"><table border="0" cellpadding="0" cellspacing="0" role="presentation" width="100%"><tbody><tr><td style="vertical-align:top;padding:0;"><table border="0" cellpadding="0" cellspacing="0" role="presentation" width="100%"><tbody><tr><td align="center" vertical-align="middle" class="respond-button" style="font-size:0px;padding:5px;word-break:break-word;"><table border="0" cellpadding="0" cellspacing="0" role="presentation" style="border-collapse:separate;width:150px;line-height:100%;"><tbody><tr><td align="center" bgcolor="#ffffff" role="presentation" valign="middle" style="border:1px solid #d9d9d9;border-radius:6px;cursor:auto;mso-padding-alt:10px 25px;background:#ffffff;"><a href="#" target="_blank" style="display:inline-block;width:100px;background:#ffffff;color:#000000;font-family:-apple-system, BlinkMacSystemFont, 'Segoe UI', 'Roboto', Helvetica, Arial, ui-sans-serif, sans-serif;font-size:15px;font-weight:600;line-height:120%;margin:0;text-decoration:none;text-transform:none;padding:10px 25px;mso-padding-alt:0px;border-radius:6px;"><span class="status-dot decline-dot"></span>Decline
          </a></td></tr></tbody></table></td></tr></tbody></table></td></tr></tbody></table></div><!--[if mso | IE]></td><td style="vertical-align:top;width:199.33331px;"><![endif]--><div class="mj-outlook-group-fix mj-column-per-33-333332" style="font-size:0px;text-align:left;direction:ltr;display:inline-block;vertical-align:top;width:100%;"><table border="0" cellpadding="0" cellspacing="0" role="presentation" width="100%"><tbody><tr><td style="vertical-align:top;padding:0;"><table border="0" cellpadding="0" cellspacing="0" role="presentation" width="100%"><tbody><tr><td align="center" vertical-align="middle" class="respond-button" style="font-size:0px;padding:5px;word-break:break-word;"><table border="0" cellpadding="0" cellspacing="0" role="presentation" style="border-collapse:separate;width:150px;line-height:100%;"><tbody><tr><td align="center" bgcolor="#ffffff" role="presentation" valign="middle" style="border:1px solid #d9d9d9;border-radius:6px;cursor:auto;mso-padding-alt:10px 25px;background:#ffffff;"><a href="#" target="_blank" style="display:inline-block;width:100px;background:#ffffff;color:#000000;font-family:-apple-system, BlinkMacSystemFont, 'Segoe UI', 'Roboto', Helvetica, Arial, ui-sans-serif, sans-serif;font-size:15px;font-weight:600;line-height:120%;margin:0;text-decoration:none;text-transform:none;padding:10px 25px;mso-padding-alt:0px;border-radius:6px;"><span class="status-dot maybe-dot"></span>Maybe
          </a></td></tr></tbody></table></td></tr></tbody></table></td></tr></tbody></table></div><!--[if mso | IE]></td></tr></table><![endif]--></td></tr></tbody></table></div><!--[if mso | IE]></td></tr></table><![endif]--><!--[if mso | IE]></td></tr><tr><td width="600px" class="default-bgcolor-outlook"><![endif]--><!--[if mso | IE]><table border="0" cellpadding="0" cellspacing="0" role="presentation" bgcolor="#ffffff" align="center" width="598" class="default-bgcolor-outlook" style="width:598px;"><tr><td style="line-height:0px;font-size:0px;mso-line-height-rule:exactly;"><![endif]--><div class="default-bgcolor" style="background:#ffffff;background-color:#ffffff;margin:0px auto;border-radius:0 0 8px 8px;max-width:598px;"><table border="0" cellpadding="0" cellspacing="0" role="presentation" align="center" style="background:#ffffff;background-color:#ffffff;width:100%;border-radius:0 0 8px 8px;"><tbody><tr><td style="direction:ltr;font-size:0px;padding:30px;text-align:center;"><!--[if mso | IE]><table border="0" cellpadding="0" cellspacing="0" role="presentation"><tr><td style="vertical-align:top;width:598px;"><![endif]--><div class="mj-outlook-group-fix mj-column-per-100" style="font-size:0px;text-align:left;direction:ltr;display:inline-block;vertical-align:top;width:100%;"><table border="0" cellpadding="0" cellspacing="0" role="presentation" width="100%"><tbody><tr><td style="vertical-align:top;padding:0;"><table border="0" cellpadding="0" cellspacing="0" role="presentation" width="100%"><tbody><tr><td align="left" class="default-color" style="font-size:0px;padding:0;word-break:break-word;"><div style="font-family:-apple-system, BlinkMacSystemFont, 'Segoe UI', 'Roboto', Helvetica, Arial, ui-sans-serif, sans-serif;font-size:13px;line-height:1.5;text-align:left;color:#262626;"><h2>Location</h2><p style="margin-top:0">place</p><h2>Notes</h2><p>some description</p></div></td></tr></tbody></table></td></tr></tbody></table></div><!--[if mso | IE]></td></tr></table><![endif]--></td></tr></tbody></table></div><!--[if mso | IE]></td></tr></table><![endif]--><!--[if mso | IE]></td></tr></table><![endif]--></td></tr></tbody></table></div><!--[if mso | IE]></td></tr></table><![endif]--><!--[if mso | IE]><table border="0" cellpadding="0" cellspacing="0" role="presentation" align="center" width="600" style="width:600px;"><tr><td style="line-height:0px;font-size:0px;mso-line-height-rule:exactly;"><![endif]--><div style="margin:0px auto;max-width:600px;"><table border="0" cellpadding="0" cellspacing="0" role="presentation" align="center" style="width:100%;"><tbody><tr><td style="direction:ltr;font-size:0px;padding:30px;text-align:center;"><!--[if mso | IE]><table border="0" cellpadding="0" cellspacing="0" role="presentation"><tr><td style="vertical-align:top;width:600px;"><![endif]--><div class="mj-outlook-group-fix mj-column-per-100" style="font-size:0px;text-align:left;direction:ltr;display:inline-block;vertical-align:top;width:100%;"><table border="0" cellpadding="0" cellspacing="0" role="presentation" width="100%"><tbody><tr><td style="vertical-align:top;padding:0;"><table border="0" cellpadding="0" cellspacing="0" role="presentation" width="100%"><tbody><tr><td align="left" style="font-size:0px;padding:0;word-break:break-word;"><div style="font-family:-apple-system, BlinkMacSystemFont, 'Segoe UI', 'Roboto', Helvetica, Arial, ui-sans-serif, sans-serif;font-size:11px;line-height:1.5;text-align:left;color:#808080;">
          This invitation is specific to you and should not be shared with others. You are receiving this email because you are an attendee on the event.<br /></div></td></tr></tbody></table></td></tr></tbody></table></div><!--[if mso | IE]></td></tr></table><![endif]--></td></tr></tbody></table></div><!--[if mso | IE]></td></tr></table><![endif]--></div></body></html>
"@


} 