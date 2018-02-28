import Component from "vue-class-component";
import Vue from 'vue'
import { HubConnection } from '@aspnet/signalr-client'
import MessagesController from "../ApiClient/MessagesController";
import MessageSummary from "../ApiClient/MessageSummary";
import BaseUrlProvider from '../BaseUrlProvider';

@Component({
    template: require('./messagelist.html')
})
export default class MessageList extends Vue {


    constructor() {
        super();
    }

    private connection: HubConnection;

    messages: MessageSummary[] = [];
    error: Error | null = null;
    selectedmessage: MessageSummary | null = null;
    loading = true;

    handleCurrentChange(message: MessageSummary | null): void {
        this.selectedmessage = message;
        this.$emit("selected-message-changed", message);
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
            this.messages = await new MessagesController().getSummaries();
        } catch (e) {
            this.error = e;

        } finally {
            this.loading = false;
        }

    }

    async created() {

        let baseUrl = new BaseUrlProvider().getBaseUrl();
        this.connection = new HubConnection(baseUrl + 'hubs/messages');

        this.connection.on('messageschanged', data => {
            this.refresh();
        });

        await this.connection.start();

        this.refresh();
    }

    async destroyed() {
        this.connection.stop();
    }
}