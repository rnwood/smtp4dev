<template>
    <div class="messagelist">

        <el-dialog v-model="composeDialogVisible" title="Send" destroy-on-close append-to-body align-center width="80%">
            <messagecompose @closed="() => composeDialogVisible=false" />
        </el-dialog>

        <div class="toolbar">


            <el-button
                       v-on:click="composeDialogVisible=true"
                       :disabled="!isRelayAvailable"
                       title="Compose">Compose</el-button>

            <el-button-group>
                <el-button icon="Delete"
                           v-on:click="deleteSelected"
                           :disabled="!selectedmessage"
                           title="Delete">Delete</el-button>

                <el-button v-on:click="relaySelected"
                           icon="d-arrow-right"
                           :disabled="!selectedmessage || !isRelayAvailable"
                           :loading="isRelayInProgress"
                           title="Relay">Relay...</el-button>
            </el-button-group>

            <el-button-group>
                <el-button icon="refresh"
                           v-on:click="refresh"
                           :disabled="loading || !selectedMailbox"
                           title="Refresh"></el-button>
                <el-button v-on:click="markAllMessageRead"
                           :disabled="loading  || !selectedMailbox"
                           title="Mark all as read">
                    <font-awesome-icon :icon="['fa-regular','envelope-open']" />
                </el-button>
                <el-button icon="close" title="Clear" @click="clear"></el-button>
            </el-button-group>


            <el-select style="flex: 1 0 200px;" v-model="selectedMailbox" class="fill">
                <el-option v-for="item in availableMailboxes"
                           :key="item.name"
                           :label="item.name"
                           :value="item.name" />

                <template #prefix>
                    <el-icon><MessageBox /></el-icon>
                </template>

            </el-select>



            <el-input class="fill"
                      v-model="searchTerm"
                      clearable
                      placeholder="Search"
                      prefix-icon="search"
                      style="flex: 1 0 150px" />

        </div>

        <el-alert v-if="error" type="error" title="Error" show-icon>
            {{ error.message }}
            <el-button v-on:click="refresh">Retry</el-button>
        </el-alert>

        <el-table :data="messages"
                  v-loading="loading"
                  :empty-text="emptyText"
                  highlight-current-row
                  @current-change="handleCurrentChange"
                  @sort-change="sort"
                  :default-sort="{ prop: 'receivedDate', order: 'descending' }"
                  class="table"
                  type="selection"
                  reserve-selection="true"
                  row-key="id"
                  :row-class-name="getRowClass"
                  ref="table"
                  stripe>
            <el-table-column property="receivedDate"
                             label="Received"
                             width="160"
                             sortable="custom"
                             :formatter="formatDate"></el-table-column>
            <el-table-column property="from"
                             label="From"
                             width="140"
                             sortable="custom"></el-table-column>
            <el-table-column property="to"
                             label="To"
                             width="180"
                             sortable="custom">
                <template #default="scope">
                    <div style="display: flow; gap: 6px;">
                        <div v-for="recip in scope.row.to" :key="recip">
                            <strong v-if="scope.row.deliveredTo.includes(recip)">{{recip}}</strong>
                            <span v-if="!scope.row.deliveredTo.includes(recip)">{{recip}}</span>
                        </div>
                    </div>
                </template>
            </el-table-column>
            <el-table-column property="isRelayed"
                             label=""
                             width="28">
                <template #default="scope">

                    <el-tooltip effect="light"
                                content="Message has been relayed"
                                placement="top-start">
                        <span> <i v-if="scope.row.isRelayed" class="fas fa-share-square"></i></span>
                    </el-tooltip>
                </template>
            </el-table-column>
            <el-table-column property="subject" label="Subject" sortable="custom">
                <template #default="scope">
                    {{ scope.row.subject }}
                    <i class="paperclip"
                       v-if="scope.row.attachmentCount"
                       :title="scope.row.attachmentCount + ' attachments'"></i>
                </template>
            </el-table-column>
        </el-table>
        <messagelistpager :paged-data="pagedServerMessages"
                          @on-current-page-change="handlePaginationCurrentChange"
                          @on-page-size-change="handlePaginationPageSizeChange"></messagelistpager>
    </div>
</template>
<script lang="ts">
    import { Component, Watch, Prop, Vue, toNative, Emit } from "vue-facing-decorator";
    import { ElMessageBox, ElNotification, TableInstance } from "element-plus";
    import MessagesController from "../ApiClient/MessagesController";
    import MessageSummary from "../ApiClient/MessageSummary";
    import HubConnectionManager from "../HubConnectionManager";
    import sortedArraySync from "../sortedArraySync";
    import { Mutex } from "async-mutex";
    import MessageNotificationManager from "../MessageNotificationManager";
    import { debounce } from "ts-debounce";
    import ServerController from "../ApiClient/ServerController";
    import ClientSettingsController from "../ApiClient/ClientSettingsController";

    import PagedResult, { EmptyPagedResult } from "@/ApiClient/PagedResult";
    import Messagelistpager from "@/components/messagelistpager.vue";
    import Mailbox from "../ApiClient/Mailbox";
    import MailboxesController from "../ApiClient/MailboxesController";
    import MessageCompose from "@/components/messagecompose.vue";


    @Component({
        components: {
            Messagelistpager,
            messagecompose: MessageCompose
        },
    })
    class MessageList extends Vue {

        private selectedSortDescending: boolean = true;
        private selectedSortColumn: string = "receivedDate";

        page: number = 1;
        pageSize: number = 25;

        pagedServerMessages: PagedResult<MessageSummary> | undefined = EmptyPagedResult<MessageSummary>();

        @Prop({ default: null })
        connection: HubConnectionManager | null = null;

        messages: MessageSummary[] = [];
        selectedMailbox: string | null = null;

        isRelayInProgress: boolean = false;
        isRelayAvailable: boolean = false;

        composeDialogVisible = false;

        get emptyText() {
            if (this.loading) {
                return "Loading";
            }

            if (!this.selectedMailbox) {
                return "Select a mailbox to view messages";
            }

            return this.searchTerm ?
                `No messages matching '${this.searchTerm}' in mailbox '${this.selectedMailbox}'`
                : `No messages in mailbox '${this.selectedMailbox}'`;
        }

        error: Error | null = null;
        selectedmessage: MessageSummary | null = null;
        searchTerm: string = "";
        loading: boolean = true;
        availableMailboxes: Mailbox[] | null = null;

        private messageNotificationManager: MessageNotificationManager | null = null;

        selectMessage(message: MessageSummary) {
            (this.$refs.table as TableInstance).setCurrentRow(message);
            this.handleCurrentChange(message);
        }

        @Emit("selected-message-changed")
        handleCurrentChange(message: MessageSummary | null) {
            this.selectedmessage = message;
            return message;
        }

        async handlePaginationCurrentChange(page: number) {
            this.page = page;
            await this.refresh(false);
        }

        async handlePaginationPageSizeChange(pageSize: number) {
            this.pageSize = pageSize;
            await this.refresh(false);
        }

        formatDate(row: number, column: number, cellValue: Date): string {
            return cellValue?.toLocaleString();
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
                let dialogResult = await ElMessageBox.prompt(
                    "Email address(es) to relay to (separate multiple with ,)",
                    "Relay Message",
                    {
                        confirmButtonText: "OK",
                        inputValue: this.selectedmessage.to.join(","),
                        cancelButtonText: "Cancel",
                        inputPattern: /[^, ]+(, *[^, ]+)*/,
                        inputErrorMessage: "Invalid email addresses",
                    }
                );

                emails = dialogResult.value.split(",").map((e) => e.trim());
            } catch {
                return;
            }

            try {
                this.isRelayInProgress = true;
                await new MessagesController().relayMessage(this.selectedmessage.id, {
                    overrideRecipientAddresses: emails,
                });

                ElNotification.success({
                    title: "Relay Message Success",
                    message: "Completed OK",
                });
            } catch (e: any) {
                const message = e.response?.data?.detail ?? e.message;

                ElNotification.error({ title: "Relay Message Failed", message: message });
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

            let nextIndex = this.messages.indexOf(messageToDelete) + 1;
            if (nextIndex < this.messages.length) {
                this.selectMessage(this.messages[nextIndex]);
            }

            try {
                await new MessagesController().delete(messageToDelete.id);
                await this.refresh(false);
            } catch (e: any) {
                ElNotification.error({
                    title: "Delete Message Failed",
                    message: e.message,
                });
            } finally {
                this.loading = false;
            }
        }

        async clear() {

            try {
                await ElMessageBox.confirm(`Delete all messages in mailbox '${this.selectedMailbox}'?`)
            } catch {
                return;
            }


            try {
                this.loading = true;
                await new MessagesController().deleteAll(this.selectedMailbox!);
                await this.refresh(true);
            } catch (e: any) {
                ElNotification.error({
                    title: "Delete All Messages Failed",
                    message: e.message,
                });
            } finally {
                this.loading = false;
            }
        }

        @Watch("searchTerm")
        doSearch = debounce(() => this.refresh(false), 200);

        @Watch("selectedMailbox")
        async onMailboxChanged() {
            this.initialMailboxLoadDone = false;
            this.messageNotificationManager = new MessageNotificationManager(this.selectedMailbox,
                (message) => {
                    this.selectMessage(message);
                    this.handleCurrentChange(message);
                }
            );
            await this.refresh(true, false);
        }

        private lastSort: string | null = null;
        private lastSortDescending: boolean = false;
        private mutex = new Mutex();

        initialMailboxLoadDone = false;

        async markAllMessageRead() {
            await new MessagesController().markAllMessageRead(this.selectedMailbox!);
        }

        async refresh(includeNotifications: boolean, silent: boolean = false) {
            this.loading = !silent;
            let unlock = await this.mutex.acquire();

            try {
                this.loading = !silent;
                this.error = null;

                const server = await new ServerController().getServer()
                this.isRelayAvailable = !!server.relaySmtpServer;

                this.availableMailboxes = await new MailboxesController().getAll();
                if (!this.selectedMailbox) {
                    this.selectedMailbox = this.availableMailboxes.find(m => m.name == server.currentUserDefaultMailboxName)?.name ?? this.availableMailboxes[this.availableMailboxes.length - 1]?.name ?? null;
                } else {
                    //Potentially removed mailbox
                    this.selectedMailbox = this.availableMailboxes.find(m => m.name == this.selectedMailbox)?.name ?? null;
                }

                // Copy in case they are mutated during the async load below
                let sortColumn = this.selectedSortColumn;
                let sortDescending = this.selectedSortDescending;

                if (!this.selectedMailbox) {
                    this.pagedServerMessages = { currentPage: 1, firstRowOnPage: 0, lastRowOnPage: 0, pageCount: 1, rowCount: 0, pageSize: this.pageSize, results: [] };
                } else {

                    this.pagedServerMessages = await new MessagesController().getSummaries(
                        this.selectedMailbox,
                        this.searchTerm,
                        sortColumn,
                        sortDescending,
                        this.page,
                        this.pageSize
                    );
                }

                if (
                    !this.lastSort ||
                    this.lastSort != sortColumn ||
                    this.lastSortDescending != sortDescending ||
                    this.pagedServerMessages.results.length == 0
                ) {
                    this.messages.splice(
                        0,
                        this.messages.length,
                        ...this.pagedServerMessages.results
                    );
                } else {
                    sortedArraySync(
                        this.pagedServerMessages.results,
                        this.messages,
                        (a: MessageSummary, b: MessageSummary) => a.id == b.id,
                        (sourceItem: MessageSummary, targetItem: MessageSummary) => {
                            targetItem.isUnread = sourceItem.isUnread;
                            targetItem.isRelayed = sourceItem.isRelayed;
                        }
                    );
                }

                if (includeNotifications) {
                    await this.messageNotificationManager?.refresh(!this.initialMailboxLoadDone);
                }

                this.initialMailboxLoadDone = true;
                this.lastSort = sortColumn;
                this.lastSortDescending = this.selectedSortDescending;
            } catch (e: any) {
                this.error = e;
            } finally {
                this.loading = false;
                unlock();
            }
        }

        async sort(sortOptions: { prop: string, order: string }) {
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

                await this.refresh(false);
            }
        }

        async mounted() {
            await this.refresh(true, false);
        }

        async created() {
            await this.initPageSizeProps();
        }

        private async initPageSizeProps() {
            const defaultPageSize = 25;
            let client = await new ClientSettingsController().getClientSettings();
            this.pageSize = client.pageSize || defaultPageSize;
        }

        @Watch("connection")
        async onConnectionChanged() {
            if (this.connection) {
                this.connection.on("messageschanged", async () => {
                    await this.refresh(true, true);
                });
                this.connection.on("serverchanged", async () => {
                    await this.refresh(true, true);
                });
                this.connection.on("mailboxeschanged", async () => {
                    await this.refresh(false, true);
                });
                this.connection.addOnConnectedCallback(() => {
                    this.refresh(true, true);
                });
            }
        }
    }


    export default toNative(MessageList)
</script>
