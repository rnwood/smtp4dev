
import User from './User';
export default class Server {

    constructor(isRunning: boolean, exception: string, portNumber: number, hostName: string, allowRemoteConnections: boolean, numberOfMessagesToKeep: number, numberOfSessionsToKeep: number, imapPortNumber: number, settingsAreEditable: boolean, disableMessageSanitisation: boolean, automaticRelayExpression: string, tlsMode: string,     credentialsValidationExpression: string,
    authenticationRequired: boolean,
        secureConnectionRequired: boolean, recipientValidationExpression: string, messageValidationExpression: string, disableIPv6: string, users: User[],
        relayTlsMode: string | undefined,
        relaySmtpServer: string,
        relaySmtpPort: number,
        relayAutomaticEmails: string[],
        relaySenderAddress: string,
        relayLogin: string,
        relayPassword: string,
        lockedSettings: { [key: string]: string }) {
         
        this.isRunning = isRunning; 
        this.exception = exception; 
        this.portNumber = portNumber; 
        this.hostName = hostName; 
        this.allowRemoteConnections = allowRemoteConnections; 
        this.numberOfMessagesToKeep = numberOfMessagesToKeep; 
        this.numberOfSessionsToKeep = numberOfSessionsToKeep; 
        this.imapPortNumber = imapPortNumber;
        this.settingsAreEditable = settingsAreEditable;
        this.disableMessageSanitisation = disableMessageSanitisation;
        this.automaticRelayExpression = automaticRelayExpression;
        this.tlsMode = tlsMode;
        this.credentialsValidationExpression = credentialsValidationExpression;
        this.authenticationRequired = authenticationRequired;
        this.secureConnectionRequired = secureConnectionRequired;
        this.recipientValidationExpression = recipientValidationExpression;
        this.messageValidationExpression = messageValidationExpression;
        this.disableIPv6 = disableIPv6;
        this.users = users;
        this.relayTlsMode = relayTlsMode;
        this.relaySmtpServer = relaySmtpServer;
        this.relaySmtpPort = relaySmtpPort;
        this.relayAutomaticEmails = relayAutomaticEmails;
        this.relaySenderAddress = relaySenderAddress;
        this.relayLogin = relayLogin;
        this.relayPassword = relayPassword;
        this.lockedSettings = lockedSettings;
    }

     
    isRunning: boolean; 
    exception: string; 
    portNumber: number; 
    hostName: string; 
    allowRemoteConnections: boolean; 
    numberOfMessagesToKeep: number; 
    numberOfSessionsToKeep: number; 
    imapPortNumber: number;
    settingsAreEditable: boolean;
    disableMessageSanitisation: boolean;
    automaticRelayExpression: string;
    tlsMode: string;
    credentialsValidationExpression: string;
    authenticationRequired: boolean;
    secureConnectionRequired: boolean;
    recipientValidationExpression: string;
    messageValidationExpression: string;
    disableIPv6: string;
    users: User[];
    relayTlsMode: string | undefined;
    relaySmtpServer: string;
    relaySmtpPort: number;
    relayAutomaticEmails: string[];
    relaySenderAddress: string;
    relayLogin: string;
    relayPassword: string;
    lockedSettings: { [key: string]: string };
}
