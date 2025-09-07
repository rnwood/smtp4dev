import { HubConnection, HubConnectionBuilder, LogLevel } from '@microsoft/signalr';
import Server from './ApiClient/Server';
import ServerController from './ApiClient/ServerController';

export default class HubConnectionManager {

    private _connection: HubConnection;
    connected: boolean = false;
    private connectedCallbacks: (() => void)[] = [];
    started: boolean = false;
    error: Error | null = null;

    constructor(url: string) {
        this._connection = new HubConnectionBuilder().withUrl(url, { logMessageContent: true }).configureLogging(LogLevel.Trace).build();
        this._connection.onclose(this.onConnectionClosed.bind(this));
        this._connection.onreconnected(() => {
            for (const connectedCallback of this.connectedCallbacks) {
                connectedCallback();
            }
        });
        this._connection.on("serverchanged", this.fireServerChanged.bind(this));
    }

    async addOnConnectedCallback(connectedCallback: () => void) {

        this.connectedCallbacks.push(connectedCallback);

        if (this.connected) {
            connectedCallback();
        }
    }

    async start() {

        if (this.started) {
            return;
        }

        this.error = null;
        this.started = true;
        try {
            await this._connection.start();
            this.connected = true;

            this.fireServerChanged();
            for (const connectedCallback of this.connectedCallbacks) {
                connectedCallback();
            }
    
        } catch (e: any) {
            this.error = e;
        }
    }

    async stop() {
        this.started = false;
        await this._connection.stop();
    }

    onConnectionClosed(e: Error | undefined) {
        this.connected = false;
        this.started = false;
        this.error = e || null;
    }


    on(eventName: string, handler: (...args: any[]) => void) {
        this._connection.on(eventName, handler);
    }

    fireServerChanged(...args: any[]) {
        console.log('HubConnectionManager: fireServerChanged called, handlers count:', this.onServerChangedHandlers.length);
        this.serverPromise = null;

        for (let handler of this.onServerChangedHandlers) {
            console.log('HubConnectionManager: calling handler');
            handler.call(this);
        }
    }

    onServerChanged(handler: (...args: any[]) => void) {
        console.log('HubConnectionManager: registering onServerChanged handler, total handlers will be:', this.onServerChangedHandlers.length + 1);
        this.onServerChangedHandlers.push(handler);
    }

    private onServerChangedHandlers: ((...args: any[]) => void)[] = []

    async getServer() {
        if (!this.serverPromise) {
            this.serverPromise = new ServerController().getServer();
        }

        return this.serverPromise;
    }

    serverPromise: Promise<Server> | null = null;



}

