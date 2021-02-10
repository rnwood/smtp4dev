<template>
    <div class="messagelist">
        <div class="toolbar">

            <confirmationdialog v-on:confirm="clear"
                                always-key="deleteAllMessages"
                                message="Are you sure you want to delete all messages?">
                <el-button icon="el-icon-close" title="Clear"></el-button>
            </confirmationdialog>

            <el-button icon="el-icon-delete"
                       v-on:click="deleteSelected"
                       :disabled="!selectedmessage" title="Delete"></el-button>
            <el-button icon="el-icon-refresh"
                       v-on:click="refresh"
                       :disabled="loading" title="Refresh"></el-button>

            <el-button v-on:click="relaySelected" icon="el-icon-d-arrow-right" :disabled="!selectedmessage || !isRelayAvailable" :loading="isRelayInProgress" title="Relay"></el-button>

            <el-input v-model="searchTerm"
                      clearable
                      placeholder="Search"
                      prefix-icon="el-icon-search"
                      style="float: right; width: 35%; min-width: 150px;" />
        </div>

        <el-alert v-if="error" type="error" title="Error" show-icon>
            {{error.message}}
            <el-button v-on:click="refresh">Retry</el-button>
        </el-alert>

        <el-table :data="filteredMessages"
                  v-loading="loading"
                  :empty-text="emptyText"
                  highlight-current-row
                  @current-change="handleCurrentChange"
                  @sort-change="sort"
                  :default-sort="{prop: 'receivedDate', order: 'descending'}"
                  class="table"
                  type="selection"
                  reserve-selection="true"
                  row-key="id"
                  :row-class-name="getRowClass"
                  ref="table"
                  stripe>
            <el-table-column property="receivedDate"
                             label="Received"
                             width="180"
                             sortable="custom"
                             :formatter="formatDate"></el-table-column>
            <el-table-column property="from" label="From" width="120" sortable="custom"></el-table-column>
            <el-table-column property="to" label="To" width="120" sortable="custom"></el-table-column>
            <el-table-column property="subject" label="Subject" sortable="custom">
                <template slot-scope="scope">
                    {{scope.row.subject}}
                    <i class="el-icon-paperclip"
                       v-if="scope.row.attachmentCount"
                       :title="scope.row.attachmentCount + ' attachments'"></i>
                </template>
            </el-table-column>
        </el-table>
    </div>
</template>
<script lang="ts">
    import { Component, Watch, Prop } from "vue-property-decorator";
    import Vue from "vue";
    import { DefaultSortOptions, ElTable } from "element-ui/types/table";
    import MessagesController from "../ApiClient/MessagesController";
    import MessageSummary from "../ApiClient/MessageSummary";
    import * as moment from 'moment';
    import HubConnectionManager from "../HubConnectionManager";
    import sortedArraySync from "../sortedArraySync";
    import { Mutex } from "async-mutex";
    import MessageNotificationManager from "../MessageNotificationManager";
    import { debounce } from "ts-debounce";

    import ConfirmationDialog from "@/components/confirmationdialog.vue";
    import { MessageBoxInputData } from 'element-ui/types/message-box';
    import ServerController from '../ApiClient/ServerController';
    import { mapOrder } from '@/components/utils/mapOrder';

    @Component({
        components: {
            confirmationdialog: ConfirmationDialog
        }
    })
    export default class MessageList extends Vue {
        constructor() {
            super();
        }

        private selectedSortDescending: boolean = true;
        private selectedSortColumn: string = "receivedDate";


        @Prop({ default: null })
        connection: HubConnectionManager | null = null;

        messages: MessageSummary[] = [];
        filteredMessages: MessageSummary[] = [];

        isRelayInProgress: boolean = false;
        isRelayAvailable: boolean = false;

        emptyText: string = "No messages";
        error: Error | null = null;
        selectedmessage: MessageSummary | null = null;
        searchTerm: string = "";
        private loading: boolean = true;
        private messageNotificationManager = new MessageNotificationManager(
            message => {
                this.selectMessage(message);
                this.handleCurrentChange(message);
            }
        );

        selectMessage(message: MessageSummary) {
            (<ElTable>this.$refs.table).setCurrentRow(message);
            this.handleCurrentChange(message);
        }

        handleCurrentChange(message: MessageSummary | null) {
            this.selectedmessage = message;
            this.$emit("selected-message-changed", message);
        }

        formatDate(
            row: number,
            column: number,
            cellValue: Date
        ): string {
            return (<any>moment)(cellValue).format("YYYY-MM-DD HH:mm:ss");
        }

        getRowClass(event: { row: MessageSummary }): string {
            return event.row.isUnread ? "unread" : "read";
        }

        async relaySelected() {
            if (this.selectedmessage == null) {
                return;
            }


            let emails: string[];

            try {

                let dialogResult = <MessageBoxInputData>await this.$prompt('Email address(es) to relay to (separate multiple with ,)', 'Relay Message', {
                    confirmButtonText: 'OK',
                    inputValue: this.selectedmessage.to,
                    cancelButtonText: 'Cancel',
                    inputPattern: /[^, ]+(, *[^, ]+)*/,
                    inputErrorMessage: 'Invalid email addresses'
                });

                emails = (<string>dialogResult.value).split(",").map(e => e.trim());
            } catch {
                return;
            }

            try {
                this.isRelayInProgress = true;
                await new MessagesController().relayMessage(this.selectedmessage.id, { overrideRecipientAddresses: emails });

                this.$notify.success({ title: "Relay Message Success", message: "Completed OK" });
            } catch (e) {
                var message = e.response?.data?.detail ?? e.sessage;

                this.$notify.error({ title: "Relay Message Failed", message: message });
            } finally {
                this.isRelayInProgress = false;
            }
        }

        async deleteSelected() {
            if (this.selectedmessage == null) {
                return;
            }

            this.loading = true;

            let messageToDelete = this.selectedmessage;

            let nextIndex = this.filteredMessages.indexOf(messageToDelete) + 1;
            if (nextIndex < this.filteredMessages.length) {
                this.selectMessage(this.filteredMessages[nextIndex]);
            }

            try {
                await new MessagesController().delete(messageToDelete.id);
                await this.refresh();
            } catch (e) {
                this.$notify.error({ title: "Delete Message Failed", message: e.message });
            } finally {
                this.loading = false;
            }
        }

        async clear() {
            try {
                this.loading = true;
                await new MessagesController().deleteAll();
                await this.refresh();
            } catch (e) {
                this.$notify.error({ title: "Clear Messages Failed", message: e.message });
            } finally {
                this.loading = false;
            }
        }

        @Watch("searchTerm")
        doSearch() {
            this.loading = true;
            this.debouncedUpdateFilteredMessages();
        }

        debouncedUpdateFilteredMessages = debounce(this.updateFilteredMessages, 200);

        updateFilteredMessages() {

            try {
                this.loading = true;
                if (this.searchTerm) {
                    this.emptyText = "No messages matching '" + this.searchTerm + "'";
                } else {
                    this.emptyText = "No messages";
                }

                sortedArraySync(
                    this.messages.filter(m => this.searchTermPredicate(m)
                    ),
                    this.filteredMessages,
                    (a: MessageSummary, b: MessageSummary) => a.id == b.id,
                    (sourceItem: MessageSummary, targetItem: MessageSummary) => {
                        targetItem.isUnread = sourceItem.isUnread;
                    }
                );
                
                const sortedMessageIds = this.messages.map(m=>m.id);
                this.filteredMessages = mapOrder(this.filteredMessages, sortedMessageIds, 'id');

                if (!this.filteredMessages.some(m => this.selectedmessage != null && m.id == this.selectedmessage.id)) {
                    this.handleCurrentChange(null);
                }
            } finally {
                this.loading = false;
            }
        }

      private searchTermPredicate(m: MessageSummary) {
        return !this.searchTerm ||
            m.subject.localeIndexOf(this.searchTerm, undefined, {
              sensitivity: "base"
            }) != -1 ||
            m.to.localeIndexOf(this.searchTerm, undefined, {
              sensitivity: "base"
            }) != -1 ||
            m.from.localeIndexOf(this.searchTerm, undefined, {
              sensitivity: "base"
            }) != -1;
      }

      private lastSort: string | null = null;
        private lastSortDescending: boolean = false;
        private mutex = new Mutex();

        initialLoadDone = false;

        async refresh(silent: boolean = false) {
            var unlock = await this.mutex.acquire();

            try {
                this.error = null;
                this.loading = !silent;


                //Copy in case they are mutated during the async load below
                let sortColumn = this.selectedSortColumn;
                let sortDescending = this.selectedSortDescending;

                let serverMessages = await new MessagesController().getSummaries(
                    sortColumn,
                    sortDescending
                );

                if (
                    !this.lastSort ||
                    this.lastSort != sortColumn ||
                    this.lastSortDescending != sortDescending ||
                    serverMessages.length == 0
                ) {
                    this.messages.splice(0, this.messages.length, ...serverMessages);
                } else {
                    sortedArraySync(
                        serverMessages,
                        this.messages,
                        (a: MessageSummary, b: MessageSummary) => a.id == b.id,
                        (sourceItem: MessageSummary, targetItem: MessageSummary) => {
                            targetItem.isUnread = sourceItem.isUnread;
                        }
                    );
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

                this.isRelayAvailable = !!await (await new ServerController().getServer()).relayOptions.smtpServer;
            } catch (e) {
                this.error = e;
            } finally {
                this.loading = false;
                unlock();
            }
        }

        async sort(sortOptions: DefaultSortOptions) {
            let descending: boolean = true;
            if (sortOptions.order === "ascending") {
                descending = false;
            }

            if (
                this.selectedSortColumn != sortOptions.prop ||
                this.selectedSortDescending != descending
            ) {
                this.selectedSortColumn = sortOptions.prop || "receivedDate";
                this.selectedSortDescending = descending;

                await this.refresh();
            }
        }


        async mounted() {
            await this.refresh(false);
        }

        @Watch("connection")
        async onConnectionChanged() {

            if (this.connection) {
                this.connection.on("messageschanged", async () => {
                    await this.refresh(true);
                });
                this.connection.on("serverchanged", async () => {
                    await this.refresh(true);
                });
                this.connection.addOnConnectedCallback(() => {
                    this.refresh(true);
                });
            }
        }

    }
</script>
