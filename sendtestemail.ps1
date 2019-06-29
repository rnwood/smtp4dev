param($count=1)

1..$count | %{ 
    write-host $_
    Send-MailMessage -To foo@bar.com -From from@from.com -Subject "Message $_" -SmtpServer localhost
}