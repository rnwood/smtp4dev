

export default class ServerRelayOptions {
 
    constructor(smtpServer: string, smtpPort: number, automaticEmails: string[], senderAddress: string, login: string, password: string, ) {
         
        this.smtpServer = smtpServer; 
        this.smtpPort = smtpPort; 
        this.automaticEmails = automaticEmails; 
        this.senderAddress = senderAddress; 
        this.login = login; 
        this.password = password;
    }

     
    smtpServer: string; 
    smtpPort: number; 
    automaticEmails: string[]; 
    senderAddress: string; 
    login: string; 
    password: string;
}
