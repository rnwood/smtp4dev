import Mailbox from './Mailbox';
import User from './User';
export default class Server {
    currentUserName: string;
    currentUserDefaultMailboxName: string;


    constructor(isRunning: boolean, exception: string, portNumber: number, hostName: string, allowRemoteConnections: boolean, numberOfMessagesToKeep: number, numberOfSessionsToKeep: number, imapPortNumber: number, pop3PortNumber: number, settingsAreEditable: boolean, disableMessageSanitisation: boolean, automaticRelayExpression: string, tlsMode: string, credentialsValidationExpression: string,
        authenticationRequired: boolean,
        secureConnectionRequired: boolean, recipientValidationExpression: string, messageValidationExpression: string, commandValidationExpression: string, disableIPv6: string, users: User[],
        pop3TlsMode: string | undefined,
        pop3SecureConnectionRequired: boolean,
        relayTlsMode: string | undefined,
        relaySmtpServer: string,
        relaySmtpPort: number,
        relayAutomaticEmails: string[],
        relaySenderAddress: string,
        relayLogin: string,
        relayPassword: string,
        webAuthenticationRequired: boolean, 
        deliverMessagesToUsersDefaultMailbox: boolean,
        smtpAllowAnyCredentials: boolean,
        lockedSettings: { [key: string]: string },
        minimiseToTrayIcon: boolean,
        isDesktopApp: boolean,
        smtpEnabledAuthTypesWhenNotSecureConnection: string[],
        smtpEnabledAuthTypesWhenSecureConnection: string[],
        mailboxes: Mailbox[],
        currentUserName: string,
        currentUserDefaultMailboxName: string,
        htmlValidateConfig: string
    ) {
        
        this.isRunning = isRunning;
        this.exception = exception;
        this.portNumber = portNumber;
        this.hostName = hostName;
        this.allowRemoteConnections = allowRemoteConnections;
        this.numberOfMessagesToKeep = numberOfMessagesToKeep;
        this.numberOfSessionsToKeep = numberOfSessionsToKeep;
        this.imapPortNumber = imapPortNumber;
        this.pop3PortNumber = pop3PortNumber;
        this.settingsAreEditable = settingsAreEditable;
        this.disableMessageSanitisation = disableMessageSanitisation;
        this.automaticRelayExpression = automaticRelayExpression;
        this.tlsMode = tlsMode;
        this.credentialsValidationExpression = credentialsValidationExpression;
        this.authenticationRequired = authenticationRequired;
        this.secureConnectionRequired = secureConnectionRequired;
        this.pop3TlsMode = pop3TlsMode;
        this.pop3SecureConnectionRequired = pop3SecureConnectionRequired;
        this.relayTlsMode = relayTlsMode;
        this.relaySmtpServer = relaySmtpServer;
        this.relaySmtpPort = relaySmtpPort;
        this.relayAutomaticEmails = relayAutomaticEmails;
        this.relaySenderAddress = relaySenderAddress;
        this.relayLogin = relayLogin;
        this.relayPassword = relayPassword;
        this.lockedSettings = lockedSettings;
        this.webAuthenticationRequired = webAuthenticationRequired;
        this.deliverMessagesToUsersDefaultMailbox = deliverMessagesToUsersDefaultMailbox;
        this.smtpAllowAnyCredentials = smtpAllowAnyCredentials;
        this.minimiseToTrayIcon = minimiseToTrayIcon;
        this.isDesktopApp = isDesktopApp;
        this.smtpEnabledAuthTypesWhenNotSecureConnection = smtpEnabledAuthTypesWhenNotSecureConnection;
        this.smtpEnabledAuthTypesWhenSecureConnection = smtpEnabledAuthTypesWhenSecureConnection;
        this.mailboxes = mailboxes;
        this.currentUserName = currentUserName;
        this.currentUserDefaultMailboxName = currentUserDefaultMailboxName;
        this.htmlValidateConfig = htmlValidateConfig;
        this.imapPort = imapPortNumber;
        this.pop3Port = pop3PortNumber;
        this.pop3TlsMode = pop3TlsMode;
        this.pop3SecureConnectionRequired = pop3SecureConnectionRequired;
        this.recipientValidationExpression = recipientValidationExpression;
        this.messageValidationExpression = messageValidationExpression;
        this.commandValidationExpression = commandValidationExpression;
        this.disableIPv6 = disableIPv6;
        this.users = users;
    }


    isRunning: boolean;
    exception: string;
    portNumber: number;
    hostName: string;
    allowRemoteConnections: boolean;
    numberOfMessagesToKeep: number;
    numberOfSessionsToKeep: number;
    imapPortNumber: number;
    pop3PortNumber: number;
    settingsAreEditable: boolean;
    disableMessageSanitisation: boolean;
    automaticRelayExpression: string;
    tlsMode: string;
    credentialsValidationExpression: string;
    authenticationRequired: boolean;
    secureConnectionRequired: boolean;
    pop3TlsMode: string | undefined;
    pop3SecureConnectionRequired: boolean;
    recipientValidationExpression: string;
    messageValidationExpression: string;
    commandValidationExpression: string;
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
    webAuthenticationRequired: boolean;
    deliverMessagesToUsersDefaultMailbox: boolean;
    smtpAllowAnyCredentials: boolean;
    minimiseToTrayIcon: boolean;
    isDesktopApp: boolean;
    smtpEnabledAuthTypesWhenNotSecureConnection: string[];
    smtpEnabledAuthTypesWhenSecureConnection: string[];
    mailboxes: Mailbox[];
    htmlValidateConfig: string;
    imapPort: number;
    pop3Port: number;
}
