1..1000 | %{ 
    write-host $_
    Send-MailMessage -To foo@bar.com -From from@from.com -Subject "Message $_" -SmtpServer localhost
}