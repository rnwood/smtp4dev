param($count=1)

[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12

[System.Net.ServicePointManager]::ServerCertificateValidationCallback = {
    param($source, $cert, $chain, $errors)
    write-host "TLS cert errors: $errors"
    return $true
}


1..$count | %{ 
    write-host $_
    Send-MailMessage -usessl -verbose -Attachments ./smtp4dev.sln -To foo2@bar.com -From from@from.com -Subject "Message $_" -SmtpServer localhost -BodyAsHtml -Body  "<h1>Hey</h1>This is a message!<p>Lorem ipsum dolor sit amet, consectetur adipiscing elit. Nulla vitae interdum arcu. Donec vel odio sed mauris luctus aliquet. Suspendisse vel magna ipsum. Praesent mollis nisi sed sapien blandit, non ultrices magna ultricies. Duis blandit tempor rutrum. Integer egestas eleifend lorem, eget volutpat tellus. Maecenas mauris dui, tincidunt vitae lacus laoreet, laoreet porta lectus. Mauris a urna justo. Phasellus porta vestibulum lorem in ultricies. Interdum et malesuada fames ac ante ipsum primis in faucibus. Sed convallis at neque in consectetur. Duis sed ipsum nec risus tempor dictum et id libero. Praesent imperdiet lacinia ullamcorper. Duis ante magna, accumsan in est in, tincidunt porta felis. Suspendisse venenatis nisi eget tellus mollis semper. Phasellus tristique elit nec ultricies tincidunt.

    <p>Sed pellentesque sit amet nibh eget pellentesque. Etiam mi lectus, elementum vitae sapien id, placerat ornare nunc. Integer sagittis, est id viverra venenatis, sapien velit pulvinar felis, in fringilla urna lorem a libero. Fusce diam magna, laoreet elementum ullamcorper non, fringilla scelerisque dolor. Aenean venenatis arcu vel orci imperdiet, eu semper sapien egestas. Pellentesque condimentum elit in congue vulputate. Phasellus sed lacus feugiat, egestas justo et, sodales lorem. Morbi fringilla bibendum ligula vel imperdiet. Sed non cursus urna, eu consectetur urna. Pellentesque habitant morbi tristique senectus et netus et malesuada fames ac turpis egestas. Phasellus dui leo, pretium eu commodo eget, sagittis vitae odio. Aenean non arcu imperdiet, malesuada massa at, porta turpis. Proin iaculis ullamcorper imperdiet. Donec lacus lacus, varius eu mauris et, eleifend convallis nisi. In ut nunc tellus. Aliquam convallis congue metus at imperdiet.
    
    <p>Integer ut ligula eu nibh hendrerit aliquam ut nec metus. Nam dignissim lacus nec augue posuere sodales. Praesent suscipit lorem vitae egestas vulputate. Vestibulum ante ipsum primis in faucibus orci luctus et ultrices posuere cubilia Curae; Nullam non nibh eget nisi dapibus interdum. Suspendisse eleifend felis quis leo tempus, eget ultricies magna cursus. Vivamus ullamcorper bibendum urna a eleifend. Donec ac faucibus massa. Maecenas vehicula, urna id commodo tincidunt, mauris velit placerat orci, ut porta nisi quam id dolor. In blandit lacus sed lacus mollis, eget iaculis quam posuere. Vivamus tristique urna quis sem faucibus aliquam. Nam scelerisque egestas risus ullamcorper consequat. Vestibulum condimentum volutpat est, vitae lobortis diam dapibus eu. Proin aliquam purus sit amet diam facilisis suscipit. Vestibulum in ipsum nunc. "
}