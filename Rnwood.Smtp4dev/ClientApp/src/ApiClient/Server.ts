
//ServerRelayOptions from Server
import ServerRelayOptions from './ServerRelayOptions';
export default class Server {
 
    constructor(isRunning: boolean, exception: string, portNumber: number, hostName: string, allowRemoteConnections: boolean, numberOfMessagesToKeep: number, numberOfSessionsToKeep: number, relayOptions: ServerRelayOptions, imapPortNumber: number, settingsAreEditable: boolean, disableMessageSanitisation: boolean,) {
         
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
}
