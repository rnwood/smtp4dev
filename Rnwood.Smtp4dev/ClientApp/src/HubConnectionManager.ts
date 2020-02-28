import { HubConnection, HubConnectionBuilder, LogLevel } from '@aspnet/signalr';

export default class HubConnectionManager {

    private _connection: HubConnection;
    connected: boolean = false;
    connectedCallback: () => void;
    started: boolean = false;
    error: Error|null = null;

    constructor(url: string, connectedCallback: () => void) {
        this._connection = new HubConnectionBuilder().withUrl(url).configureLogging(LogLevel.Trace).build();
        this._connection.onclose(this.onConnectionClosed.bind(this));
        this.connectedCallback = connectedCallback;
        //this._connection.serverTimeoutInMilliseconds = 5000;
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
            this.connectedCallback();
        } catch (e) {
            this.error = e;
        }
    }

    async stop() {
        this.started = false;
        await this._connection.stop();
    }

    onConnectionClosed(e: Error|undefined) {
        this.connected = false;
        this.started = false;
        this.error = e || null;
    }


    on(eventName: string, handler: (...args: any[]) => void) {
        this._connection.on(eventName, handler);
    }



}