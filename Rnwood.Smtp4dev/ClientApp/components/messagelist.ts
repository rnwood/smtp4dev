import { Component } from 'vue-property-decorator';
import Vue from 'vue';
import { DefaultSortOptions, ElTable } from 'element-ui/types/table';
import MessagesController from "../ApiClient/MessagesController";
import MessageSummary from "../ApiClient/MessageSummary";
import * as moment from 'moment';
import HubConnectionManager from '../HubConnectionManager';
import sortedArraySync from '../sortedArraySync';
import { Mutex } from 'async-mutex';
import Message from '../ApiClient/Message';


@Component({
    components: {
        hubconnstatus: (<any>require('./hubconnectionstatus.vue.html')).default
    }
})
export default class MessageList extends Vue {


    constructor() {
        super();

        if (Notification.permission == "default") {
            Notification.requestPermission();
        }

        this.connection = new HubConnectionManager('/hubs/messages', this.refresh);
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
    loading = false;

    handleCurrentChange(message: MessageSummary | null): void {
        this.selectedmessage = message;
        this.$emit("selected-message-changed", message);
    }

    formatDate(row: number, column: number, cellValue: string, index: number): string {
        return moment(String(cellValue)).format('YYYY-MM-DD HH:mm:ss');
    }

    getRowClass(event: { row: MessageSummary }): string {
        return event.row.isUnread ? "unread" : "read";
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

    private lastSort: string | null = null;
    private lastSortDescending: boolean = false;
    private mutex = new Mutex();
    private initialLoadDone = false;

    refresh = async () => {
        var unlock = await this.mutex.acquire();
        

        try {

            this.error = null;
            this.loading = true;


            //Copy in case they are mutated during the async load below
            let sortColumn = this.selectedSortColumn;
            let sortDescending = this.selectedSortDescending;

           
            var serverMessages = await new MessagesController().getSummaries(sortColumn, sortDescending);

            var newMessages = serverMessages.filter((sm) => !this.messages.find(cm => cm.id == sm.id));

            if (!this.lastSort || this.lastSort != sortColumn || this.lastSortDescending != sortDescending || serverMessages.length == 0) {
                this.messages.splice(0, this.messages.length, ...serverMessages);
            } else {

                sortedArraySync(serverMessages, this.messages,
                    (a: MessageSummary, b: MessageSummary) => a.id == b.id, 
                    (sourceItem: MessageSummary, targetItem: MessageSummary) => {
                        targetItem.isUnread = sourceItem.isUnread;
                    });
            }


            if (this.initialLoadDone && Notification.permission == "granted") {

                for (let newMessage of newMessages) {
                    var notification = new Notification("smtp4dev: New message", {
                        body: "From: " + newMessage.from + "\nSubject: " + newMessage.subject,
                    });
                    notification.onclick = () => {
                        (<ElTable>this.$refs.table).setCurrentRow(newMessage);
                        this.handleCurrentChange(newMessage);
                    };
                    setTimeout(() => notification.close(), 5000);
                }
            }

            this.initialLoadDone = true;
            this.lastSort = sortColumn;
            this.lastSortDescending = this.selectedSortDescending;

            
        } catch (e) {
            this.error = e;

        } finally {
            this.loading = false;
            unlock();
        }

    }

    sort = async (sortOptions: DefaultSortOptions) => {
        let descending: boolean = true;
        if (sortOptions.order === "ascending") {
            descending = false;
        }

        if (this.selectedSortColumn != sortOptions.prop || this.selectedSortDescending != descending) {

            this.selectedSortColumn = sortOptions.prop || "receivedDate";
            this.selectedSortDescending = descending;

            await this.refresh();
        }
    }

    async created() {

        await this.refresh();
    }

    async destroyed() {
        this.connection.stop();
    }
}
