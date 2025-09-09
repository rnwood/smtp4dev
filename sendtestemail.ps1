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
    Send-MailMessage -credential (get-credential) -verbose -Attachments ./smtp4dev.sln -To to@bar.com -Cc cc@bar.com -bcc bcc@bar.com -From from@from.com -Subject "Message $i" -SmtpServer localhost -BodyAsHtml -Body ("BARE N`nBARE R`r" + @"
  
    <!DOCTYPE html>
<html>
<head>
<meta name="supported-color-schemes" content="light dark">
<meta name="color-scheme" content="light dark">
    <style>
        .test-section {
            padding: 20px;
            margin: 10px;
            border: 2px solid #000;
            font-weight: bold;
            font-size: 16px;
        }
        .red-section { background-color: #FF0000; color: #FFFFFF; }
        .green-section { background-color: #00FF00; color: #000000; }
        .yellow-section { background-color: #FFFF00; color: #000000; }
        .email-content { 
            background-color: #ffffff;
            color: #000000;
            padding: 20px;
        }
@media (prefers-color-scheme: dark) {



                    .email-content { background-color: #1a1a1a; color: #ffffff;}


                  }
    </style>
</head>
<body class="email-content">
    <h1>Email WITH Dark Mode Support</h1>
    <p>This email includes proper dark mode support with the meta tags and CSS media queries for dark mode.</p>
    
    <div class="test-section red-section" style="background-color: #FF0000; color: #FFFFFF; padding: 20px; margin: 10px; border: 2px solid #000; font-weight: bold; font-size: 16px;">
        Red Section - This should be RED in light mode
    </div>
    
    <div class="test-section green-section" style="background-color: #00FF00; color: #000000; padding: 20px; margin: 10px; border: 2px solid #000; font-weight: bold; font-size: 16px;">
        Green Section - This should be GREEN in light mode
    </div>
    
    <div class="test-section yellow-section" style="background-color: #FFFF00; color: #000000; padding: 20px; margin: 10px; border: 2px solid #000; font-weight: bold; font-size: 16px;">
        Yellow Section - This should be YELLOW in light mode
    </div>
        
    <p><strong>Expected behavior:</strong></p>
    <ul>
        <li><strong>Light UI:</strong> Both email types display with their original colors</li>
        <li><strong>Dark UI:</strong> 
            <ul>
                <li>Emails WITHOUT dark support are inverted (red→cyan, green→magenta, yellow→purple)</li>
                <li>Emails WITH dark support maintain their intended dark mode appearance</li>
            </ul>
        </li>
    </ul>
</body>
</html>
"@)

} 