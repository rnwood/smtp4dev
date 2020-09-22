

export default class ServerRelayOptions {
 
    constructor(smtpServer: string, smtpPort: number, allowedEmails: string[], senderAddress: string, login: string, password: string, ) {
         
        this.smtpServer = smtpServer; 
        this.smtpPort = smtpPort; 
        this.allowedEmails = allowedEmails; 
        this.senderAddress = senderAddress; 
        this.login = login; 
        this.password = password;
    }

     
    smtpServer: string; 
    smtpPort: number; 
    allowedEmails: string[]; 
    senderAddress: string; 
    login: string; 
    password: string;
}
