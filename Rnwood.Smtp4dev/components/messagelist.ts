import Component from "vue-class-component";
import Vue from 'vue'
import axios from 'axios';
import { HubConnection } from '@aspnet/signalr-client'
import Message = Api.Message;

@Component({
    template: require('./messagelist.html'),
    props: ["selectedmessge"]
})
export default class MessageList extends Vue {


    constructor() {
        super();
    }

    private connection: HubConnection;

    messages: Message[] = [];
    error?: Error;
    selectedmessage?: Message;

    handleCurrentChange(message: Message): void {
        this.selectedmessage = message; 
    }

    clear(): void {

        axios.delete("/api/messages/*")
            .then(response => {
                this.refresh();
            })
            .catch(e => {
                this.error = e
            })
    }

    refresh(): void {

        this.error;

        axios.get("/api/messages")
            .then(response => {
                this.messages = response.data as Message[];
            })
            .catch(e => {
                this.error = e;
            })

    }

    async created() {

        this.connection = new HubConnection('/hubs/messages');

        this.connection.on('messageadded', data => {
            this.refresh();
        });

        this.connection.on('messageremoved', data => {
            this.refresh();
        });

        await this.connection.start();

        this.refresh();
    }

    async destroyed() {
        this.connection.stop();
    }
}