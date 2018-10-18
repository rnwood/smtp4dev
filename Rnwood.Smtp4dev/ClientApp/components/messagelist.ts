import { Component } from 'vue-property-decorator';
import Vue from 'vue';
import { DefaultSortOptions } from 'element-ui/types/table';
import MessagesController from "../ApiClient/MessagesController";
import MessageSummary from "../ApiClient/MessageSummary";
import * as moment from 'moment';
import HubConnectionManager from '../HubConnectionManager';


@Component({
    components: {
        hubconnstatus: (<any>require('./hubconnectionstatus.vue.html')).default
    }
})
export default class MessageList extends Vue {


    constructor() {
        super();

        this.connection = new HubConnectionManager('/hubs/messages');
        this.connection.on('messageschanged', () => {
            this.refresh();
        });
        this.connection.start();
    }

    private selectedSortDescending: boolean = true;
    private selectedSortColumn: string = "receivedDate";

    connection: HubConnectionManager;
    messages: MessageSummary[] = [];
    error: Error | null = null;
    selectedmessage: MessageSummary | null = null;
    loading = true;

    handleCurrentChange(message: MessageSummary | null): void {
        this.selectedmessage = message;
        this.$emit("selected-message-changed", message);
    }

    formatDate(row: number, column: number, cellValue: string, index: number): string {
        return moment(String(cellValue)).format('YYYY-DD-MM hh:mm:ss');
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
        this.loading = true;

        try {
            this.messages = await new MessagesController().getSummaries(this.selectedSortColumn, this.selectedSortDescending);
            
        } catch (e) {
            this.error = e;

        } finally {
            this.loading = false;
        }

    }

    async sort(defaultSortOptions: DefaultSortOptions) {
        let descending: boolean = true;
        if (defaultSortOptions.order === "ascending") {
            descending = false;
        }

        this.selectedSortColumn = defaultSortOptions.prop;
        this.selectedSortDescending = descending;

        this.refresh();
    }

    async created() {

        this.refresh();
    }

    async destroyed() {
        this.connection.stop();
    }
}