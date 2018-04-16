import Component from "vue-class-component";
import Vue from 'vue'
import { HubConnection } from '@aspnet/signalr'
import MessagesController from "../ApiClient/MessagesController";
import MessageSummary from "../ApiClient/MessageSummary";

@Component({
    template: require('./messagelist.html')
})
export default class MessageList extends Vue {


    constructor() {
        super();

        this.connection = new HubConnection('/hubs/messages');

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
                await this.connection.start();
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