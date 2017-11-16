import Component from "vue-class-component";
import Vue from 'vue'
import { HubConnection } from '@aspnet/signalr-client'
import MessagesController from "../ApiClient/MessagesController";
import MessageHeader from "../ApiClient/MessageHeader";

@Component({
    template: require('./messagelist.html'),
    props: ["selectedmessge"]
})
export default class MessageList extends Vue {


    constructor() {
        super();
    }

    private connection: HubConnection;

    messages: MessageHeader[] = [];
    error: Error | null = null;
    selectedmessage: MessageHeader | null = null;
    loading = true;

    handleCurrentChange(message: MessageHeader | null): void {
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

        this.error;

        try {
            this.messages = await new MessagesController().getHeaders();
        } catch (e) {
            this.error = e;

        } finally {
            this.loading = false;
        }

    }

    async created() {

        this.connection = new HubConnection('/hubs/messages');

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