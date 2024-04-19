
//ServerRelayOptions from Server
import ServerRelayOptions from './ServerRelayOptions';
export default class Server {
 
    constructor(isRunning: boolean, exception: string, portNumber: number, hostName: string, allowRemoteConnections: boolean, numberOfMessagesToKeep: number, numberOfSessionsToKeep: number, relayOptions: ServerRelayOptions, imapPortNumber: number, settingsAreEditable: boolean, disableMessageSanitisation: boolean, automaticRelayExpression: string, tlsMode: string,     credentialsValidationExpression: string,
    authenticationRequired: boolean,
        secureConnectionRequired: boolean, recipientValidationExpression: string, messageValidationExpression: string, disableIPv6: string) {
         
        this.isRunning = isRunning; 
        this.exception = exception; 
        this.portNumber = portNumber; 
        this.hostName = hostName; 
        this.allowRemoteConnections = allowRemoteConnections; 
        this.numberOfMessagesToKeep = numberOfMessagesToKeep; 
        this.numberOfSessionsToKeep = numberOfSessionsToKeep; 
        this.relayOptions = relayOptions; 
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
    }

     
    isRunning: boolean; 
    exception: string; 
    portNumber: number; 
    hostName: string; 
    allowRemoteConnections: boolean; 
    numberOfMessagesToKeep: number; 
    numberOfSessionsToKeep: number; 
    relayOptions: ServerRelayOptions; 
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
}
