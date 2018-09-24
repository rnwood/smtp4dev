import Component from "vue-class-component";
import Vue from 'vue'
import { HubConnectionBuilder, HubConnection } from '@aspnet/signalr'
import MessagesController from "../ApiClient/MessagesController";
import MessageSummary from "../ApiClient/MessageSummary";
import Index = require("@aspnet/signalr/dist/esm/index");

@Component({
    template: require('./messagelist.html')
})
export default class MessageList extends Vue {


    constructor() {
        super();

        this.connection = new HubConnectionBuilder().withUrl('/hubs/messages').build();
        this.connection.on('messageschanged', data => {
            this.refresh();
        });
    }

    private connection: HubConnection;
    private connectionStarted = false;

    messages: MessageSummary[] = [];
    error: Error | null = null;
    selectedmessage: MessageSummary | null = null;
    loading = true;

    handleCurrentChange(message: MessageSummary | null): void {
        this.selectedmessage = message;
        this.$emit("selected-message-changed", message);
    }

    async deleteSelected() {

        if (this.selectedmessage == null) {
            return;
        }

        try {
            await new MessagesController().delete(this.selectedmessage.id);
            this.refresh();
        } catch (e) {
            this.error = e;
        }

    }

    async clear() {

        try {
            await new MessagesController().deleteAll();
            this.refresh();
        } catch (e) {
            this.error = e;
        }

    }

    async refresh() {

        this.error = null;

        try {
            if (!this.connectionStarted) {
                await this.connection
                    .start()
                    .then(() => console.log('Message connection started.'))
                    .catch(err => console.log('Error establishing connection (' + err + ')'));
                this.connectionStarted = true;
            }
            this.messages = await new MessagesController().getSummaries();
        } catch (e) {
            try {
                // if websocket is not working, display messages
                this.messages = await new MessagesController().getSummaries();
            } catch (e) {
                this.error = e;
            }
            this.error = e;
        } finally {
            this.loading = false;
        }

    }

    async created() {

        this.refresh();
    }

    async destroyed() {
        this.connection.stop();
        this.connectionStarted = false;
    }
}