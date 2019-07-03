import { Component, Watch } from 'vue-property-decorator';
import Vue from 'vue';
import { DefaultSortOptions, ElTable } from 'element-ui/types/table';
import MessagesController from "../ApiClient/MessagesController";
import MessageSummary from "../ApiClient/MessageSummary";
import * as moment from 'moment';
import HubConnectionManager from '../HubConnectionManager';
import sortedArraySync from '../sortedArraySync';
import { Mutex } from 'async-mutex';
import MessageNotificationManager from "../MessageNotificationManager";
import { debounce } from 'ts-debounce';
import localeindexof from 'locale-index-of';

@Component({
    components: {
        hubconnstatus: (<any>require('./hubconnectionstatus.vue.html')).default
    }
})
export default class MessageList extends Vue {


    constructor() {
        super();

        this.connection = new HubConnectionManager('hubs/messages', this.refresh);
        this.connection.on('messageschanged', async () => {
            await this.refresh();
        });
        this.connection.start();
    }

    private selectedSortDescending: boolean = true;
    private selectedSortColumn: string = "receivedDate";
    private emptyText = "No messages";

    connection: HubConnectionManager;
    messages: MessageSummary[] = [];
    filteredMessages: MessageSummary[] = [];

    error: Error | null = null;
    selectedmessage: MessageSummary | null = null;
    searchTerm: string = "";
    loading = false;
    private messageNotificationManager = new MessageNotificationManager(message => {
        (<ElTable>this.$refs.table).setCurrentRow(message);
        this.handleCurrentChange(message);
    });

    handleCurrentChange(message: MessageSummary | null): void {
        this.selectedmessage = message;
        this.$emit("selected-message-changed", message);
    }

    formatDate(row: number, column: number, cellValue: Date, index: number): string {
        return moment(cellValue).format('YYYY-MM-DD HH:mm:ss');
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
            await this.refresh();
        } catch (e) {
            this.error = e;
        }

    }

    async clear() {

        try {
            await new MessagesController().deleteAll();
            await this.refresh();
        } catch (e) {
            this.error = e;
        }
    }

    @Watch("searchTerm")
    doSearch() {
        this.debouncedUpdateFilteredMessages();
    }

    debouncedUpdateFilteredMessages = debounce(this.updateFilteredMessages, 200);

    updateFilteredMessages() {
        this.emptyText = "No messages matching '" + this.searchTerm + "'";

        sortedArraySync(this.messages.filter(m =>
            !this.searchTerm ||
            m.subject.localeIndexOf(this.searchTerm, undefined, { sensitivity: "base" }) != -1 ||
            m.to.localeIndexOf(this.searchTerm, undefined, { sensitivity: "base" }) != -1 || 
            m.from.localeIndexOf(this.searchTerm, undefined, { sensitivity: "base" }) != -1
            ),
            this.filteredMessages,
            (a: MessageSummary, b: MessageSummary) => a.id == b.id,
            (sourceItem: MessageSummary, targetItem: MessageSummary) => {
                targetItem.isUnread = sourceItem.isUnread;
            });
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


            let serverMessages = await new MessagesController().getSummaries(sortColumn, sortDescending);

            if (!this.lastSort || this.lastSort != sortColumn || this.lastSortDescending != sortDescending || serverMessages.length == 0) {
                this.messages.splice(0, this.messages.length, ...serverMessages);
            } else {

                sortedArraySync(serverMessages, this.messages,
                    (a: MessageSummary, b: MessageSummary) => a.id == b.id,
                    (sourceItem: MessageSummary, targetItem: MessageSummary) => {
                        targetItem.isUnread = sourceItem.isUnread;
                    });
            }

            if (this.initialLoadDone) {

                this.messageNotificationManager.notifyMessages(this.messages);
            } else {
                this.messageNotificationManager.setInitialMessages(this.messages);
            }

            this.updateFilteredMessages();

            this.initialLoadDone = true;
            this.lastSort = sortColumn;
            this.lastSortDescending = this.selectedSortDescending;


        } catch (e) {
            console.error(e);
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
        await this.connection.stop();
    }
}
