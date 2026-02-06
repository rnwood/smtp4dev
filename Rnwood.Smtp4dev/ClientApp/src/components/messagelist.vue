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

            <el-button
                       v-on:click="showFileSelector"
                       icon="Upload"
                       :disabled="!selectedMailbox"
                       title="Import EML files">Import</el-button>

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

            <el-select style="flex: 1 0 150px;" v-model="selectedFolder" class="fill" :disabled="!selectedMailbox">
                <el-option v-for="folder in availableFolders"
                           :key="folder"
                           :label="folder"
                           :value="folder" />

                <template #prefix>
                    <el-icon><Files /></el-icon>
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
                            <strong v-if="(scope.row.deliveredTo??'').includes(recip)">{{recip}}</strong>
                            <span v-if="!(scope.row.deliveredTo??'').includes(recip)">{{recip}}</span>
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
            <el-table-column property="hasWarnings"
                             label=""
                             width="28">
                <template #default="scope">
                    <el-tooltip effect="light"
                                content="Message has warnings"
                                placement="top-start">
                        <el-icon v-if="scope.row.hasWarnings" style="color: orange;">
                            <Warning />
                        </el-icon>
                    </el-tooltip>
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
    import ClientSettingsManager from "../ApiClient/ClientSettingsManager";

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
        private static readonly MAILBOX_STORAGE_KEY = "smtp4dev-selected-mailbox";
        private isInitialLoad: boolean = true;
        private suppressRouteUpdate: boolean = false;

        page: number = 1;

        pagedServerMessages: PagedResult<MessageSummary> | undefined = EmptyPagedResult<MessageSummary>();

        @Prop({ default: null })
        connection: HubConnectionManager | null = null;

        messages: MessageSummary[] = [];
        selectedMailbox: string | null = null;
        selectedFolder: string = "INBOX"; // Default to INBOX folder
        availableFolders: string[] = [];

        isRelayInProgress: boolean = false;
        isRelayAvailable: boolean = false;

        composeDialogVisible = false;
        pendingSelectMessageAfterRefresh: MessageSummary | null = null;

        get emptyText() {
            if (this.loading) {
                return "Loading";
            }

            if (!this.selectedMailbox) {
                return "Select a mailbox to view messages";
            }

            const folderText = this.selectedFolder ? ` in folder '${this.selectedFolder}'` : '';
            return this.searchTerm ?
                `No messages matching '${this.searchTerm}' in mailbox '${this.selectedMailbox}'${folderText}`
                : `No messages in mailbox '${this.selectedMailbox}'${folderText}`;
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
            
            // Update URL with selected message
            if (!this.suppressRouteUpdate && !this.isInitialLoad) {
                this.updateRouteWithCurrentState();
            }
            
            return message;
        }

        async handlePaginationCurrentChange(page: number) {
            this.page = page;
            await this.refresh(false);
        }

        async handlePaginationPageSizeChange(pageSize: number) {
            ClientSettingsManager.updateClientSettings({ pageSize });
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

        showFileSelector() {
            // Create a hidden file input element
            const fileInput = document.createElement('input');
            fileInput.type = 'file';
            fileInput.accept = '.eml';
            fileInput.multiple = true;
            fileInput.style.display = 'none';
            
            // Handle file selection
            fileInput.addEventListener('change', (event: any) => {
                const files = event.target.files;
                if (files && files.length > 0) {
                    this.importFiles(Array.from(files));
                }
                // Clean up the temporary element
                document.body.removeChild(fileInput);
            });
            
            // Add to DOM and trigger click
            document.body.appendChild(fileInput);
            fileInput.click();
        }

        async importFiles(files: File[]) {
            if (files.length === 0) {
                return;
            }

            let successCount = 0;
            let failCount = 0;
            const importedIds: string[] = [];

            // Show initial progress notification and store reference to dismiss it later
            const progressNotification = ElNotification.info({
                title: "Import in Progress",
                message: `Importing ${files.length} file(s)...`,
                duration: 0  // Don't auto-dismiss, we'll handle it manually
            });

            try {
                for (const file of files) {
                    try {
                        // Read file content as text
                        const emlContent = await this.readFileAsText(file);
                        
                        // Call the import API for this single file
                        const messageId = await new MessagesController().import(emlContent, this.selectedMailbox);
                        
                        successCount++;
                        importedIds.push(messageId);
                        
                    } catch (e: any) {
                        failCount++;
                        const message = e.response?.data ?? e.message;
                        ElNotification.error({ 
                            title: "Import Failed", 
                            message: `${file.name}: ${message}`,
                            duration: 5000
                        });
                    }
                }

                // Show final success notification
                if (successCount > 0) {
                    ElNotification.success({
                        title: "Import Complete",
                        message: `Successfully imported ${successCount} of ${files.length} files`,
                        duration: 4000
                    });

                    // Refresh the message list
                    await this.refresh(false);
                    
                    // Select the first imported message by its specific ID with retry logic
                    if (importedIds.length > 0) {
                        const firstImportedId = importedIds[0];
                        
                        // Retry mechanism to wait for the imported message to appear in the list
                        let retryCount = 0;
                        const maxRetries = 10;
                        const retryDelay = 200; // 200ms between retries
                        
                        while (retryCount < maxRetries) {
                            const importedMessage = this.messages.find(m => m.id === firstImportedId);
                            if (importedMessage) {
                                this.selectMessage(importedMessage);
                                break;
                            }
                            
                            // Wait before retrying
                            await new Promise(resolve => setTimeout(resolve, retryDelay));
                            retryCount++;
                        }
                    }
                }
            } finally {
                // Always dismiss the progress notification when import completes
                progressNotification.close();
            }
        }

        readFileAsText(file: File): Promise<string> {
            return new Promise((resolve, reject) => {
                const reader = new FileReader();
                reader.onload = (e) => resolve(e.target?.result as string);
                reader.onerror = (e) => reject(new Error('Failed to read file'));
                reader.readAsText(file);
            });
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

        private debouncedRefresh = debounce(() => this.refresh(false), 200);

        @Watch("searchTerm")
        onSearchTermChanged() {
            this.debouncedDoSearch();
            // Update URL with search filter
            if (!this.suppressRouteUpdate && !this.isInitialLoad) {
                this.debouncedUpdateRoute();
            }
        }
        
        debouncedDoSearch = debounce(() => this.refresh(false), 200);
        debouncedUpdateRoute = debounce(() => this.updateRouteWithCurrentState(), 300);

        @Watch("selectedMailbox")
        async onMailboxChanged() {
            this.initialMailboxLoadDone = false;
            this.messageNotificationManager = new MessageNotificationManager(this.selectedMailbox,
                (message) => {
                    if (this.selectedFolder && this.selectedFolder != "INBOX") {
                        this.selectedFolder = "INBOX";
                        this.pendingSelectMessageAfterRefresh = message;
                        return;
                    }

                    this.selectMessage(message);
                    this.handleCurrentChange(message);
                }
            );
            // Default to INBOX folder when mailbox changes
            this.selectedFolder = "INBOX";
            await this.loadFolders();
            await this.refresh(true, false);

            // Save selected mailbox to localStorage
            if (this.selectedMailbox) {
                localStorage.setItem(MessageList.MAILBOX_STORAGE_KEY, this.selectedMailbox);
            } else {
                localStorage.removeItem(MessageList.MAILBOX_STORAGE_KEY);
            }

            // Update URL route if not during initial load and not suppressed
            if (!this.isInitialLoad && !this.suppressRouteUpdate) {
                this.updateRouteWithCurrentState();
            }
        }

        @Watch("selectedFolder")
        async onFolderChanged() {
            await this.refresh(false);
            // Update URL with folder filter
            if (!this.suppressRouteUpdate && !this.isInitialLoad) {
                this.updateRouteWithCurrentState();
            }
        }

        async loadFolders() {
            if (this.selectedMailbox) {
                try {
                    this.availableFolders = await new MessagesController().getFolders(this.selectedMailbox);
                    // Ensure INBOX folder exists, if not fall back to first available folder
                    if (this.availableFolders.length > 0 && !this.availableFolders.includes("INBOX")) {
                        this.selectedFolder = this.availableFolders[0];
                    }
                } catch (error) {
                    console.error("Failed to load folders:", error);
                    this.availableFolders = [];
                }
            } else {
                this.availableFolders = [];
            }
        }

        private lastSort: string | null = null;
        private lastSortDescending: boolean = false;
        private mutex = new Mutex();

        initialMailboxLoadDone = false;

        async markAllMessageRead() {
            await new MessagesController().markAllMessageRead(this.selectedMailbox!);
        }

        async refresh(includeNotifications: boolean, silent: boolean = false) {
            if (!silent) this.loading = true;
            let unlock = await this.mutex.acquire();
            try {
                this.error = null;

                const server = await this.connection!.getServer()
                this.isRelayAvailable = !!server.relaySmtpServer;

                const mailboxes = await new MailboxesController().getAll();
                this.availableMailboxes = mailboxes.sort((a, b) => a.name.localeCompare(b.name));
                if (!this.selectedMailbox) {
                    // Priority order for mailbox selection on initial load:
                    // 1. URL route parameter (for bookmarks/direct links)
                    // 2. localStorage (for persistent selection)
                    // 3. Server default mailbox
                    // 4. Last mailbox alphabetically
                    let mailboxToSelect: string | null = null;
                    
                    if (this.isInitialLoad) {
                        // Check URL route parameter first
                        const routeMailbox = this.$route.params.mailbox as string | undefined;
                        if (routeMailbox) {
                            const decodedMailbox = decodeURIComponent(routeMailbox);
                            mailboxToSelect = this.availableMailboxes.find(m => m.name === decodedMailbox)?.name ?? null;
                        }
                        
                        // If not in URL, check localStorage
                        if (!mailboxToSelect) {
                            const storedMailbox = localStorage.getItem(MessageList.MAILBOX_STORAGE_KEY);
                            if (storedMailbox) {
                                mailboxToSelect = this.availableMailboxes.find(m => m.name === storedMailbox)?.name ?? null;
                            }
                        }
                    }
                    
                    // Fall back to server default or last mailbox
                    if (!mailboxToSelect) {
                        mailboxToSelect = this.availableMailboxes.find(m => m.name == server.currentUserDefaultMailboxName)?.name ?? this.availableMailboxes[this.availableMailboxes.length - 1]?.name ?? null;
                    }
                    
                    // Suppress route update during initial load to avoid redundant navigation
                    this.suppressRouteUpdate = true;
                    this.selectedMailbox = mailboxToSelect;
                    
                    // On initial load, restore filters from URL query parameters
                    if (this.isInitialLoad) {
                        const query = this.$route.query;
                        if (query.search) {
                            this.searchTerm = query.search as string;
                        }
                        if (query.folder) {
                            this.selectedFolder = query.folder as string;
                        }
                        if (query.sort) {
                            this.selectedSortColumn = query.sort as string;
                        }
                        if (query.order) {
                            this.selectedSortDescending = query.order !== 'asc';
                        }
                    }
                    
                    this.suppressRouteUpdate = false;
                } else {
                    //Potentially removed mailbox
                    this.selectedMailbox = this.availableMailboxes.find(m => m.name == this.selectedMailbox)?.name ?? null;
                }

                // Get current pageSize from settings
                const clientSettings = await ClientSettingsManager.getClientSettings();
                const pageSize = clientSettings.pageSize || 25;

                // Copy in case they are mutated during the async load below
                let sortColumn = this.selectedSortColumn;
                let sortDescending = this.selectedSortDescending;

                if (!this.selectedMailbox) {
                    this.pagedServerMessages = { currentPage: 1, firstRowOnPage: 0, lastRowOnPage: 0, pageCount: 1, rowCount: 0, pageSize: pageSize, results: [] };
                } else {

                    this.pagedServerMessages = await new MessagesController().getSummaries(
                        this.selectedMailbox,
                        this.searchTerm,
                        sortColumn,
                        sortDescending,
                        this.page,
                        pageSize,
                        this.selectedFolder || null
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

                if (this.pendingSelectMessageAfterRefresh) {
                    this.selectMessage(this.pendingSelectMessageAfterRefresh);
                    this.pendingSelectMessageAfterRefresh = null;
                }
                
                // On initial load, select message from URL if specified
                if (this.isInitialLoad) {
                    const messageId = this.$route.params.messageId as string | undefined;
                    if (messageId) {
                        const message = this.messages.find(m => m.id === messageId);
                        if (message) {
                            this.suppressRouteUpdate = true;
                            this.selectMessage(message);
                            this.suppressRouteUpdate = false;
                        }
                    }
                }

                this.initialMailboxLoadDone = true;
                this.lastSort = sortColumn;
                this.lastSortDescending = this.selectedSortDescending;
            } catch (e: any) {
                this.error = e;
            } finally {
                if (!silent) this.loading = false;
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
                
                // Update URL with sort parameters
                if (!this.suppressRouteUpdate && !this.isInitialLoad) {
                    this.updateRouteWithCurrentState();
                }
            }
        }

        // Method to update route with all current state (mailbox, message, filters)
        updateRouteWithCurrentState() {
            if (!this.selectedMailbox) {
                try {
                    this.$router.replace({ path: '/messages' });
                } catch (e) {
                    console.error("Error updating route:", e);
                }
                return;
            }

            // Build the path
            let path = `/messages/mailbox/${encodeURIComponent(this.selectedMailbox)}`;
            if (this.selectedmessage) {
                path += `/message/${this.selectedmessage.id}`;
            }

            // Build query parameters for filters
            const query: any = {};
            if (this.searchTerm) {
                query.search = this.searchTerm;
            }
            if (this.selectedFolder && this.selectedFolder !== 'INBOX') {
                query.folder = this.selectedFolder;
            }
            if (this.selectedSortColumn !== 'receivedDate') {
                query.sort = this.selectedSortColumn;
            }
            if (!this.selectedSortDescending) {
                query.order = 'asc';
            }

            try {
                const result = this.$router.replace({ path, query });
                // Handle the promise if it's returned
                if (result && typeof result.catch === 'function') {
                    result.catch((err: any) => {
                        // Ignore navigation cancelled errors
                        if (err && err.name !== 'NavigationDuplicated' && err.type !== 16) {
                            console.error("Error updating route:", err);
                        }
                    });
                }
            } catch (e) {
                console.error("Error updating route:", e);
            }
        }

        async mounted() {
            this.loading = true;
            await this.refresh(true, false);
            this.isInitialLoad = false;
        }

        @Watch("$route")
        onRouteChanged(newRoute: any, oldRoute: any) {
            // Handle route changes (e.g., browser back/forward)
            const newMailbox = newRoute.params.mailbox as string | undefined;
            const oldMailbox = oldRoute.params.mailbox as string | undefined;
            const newMessageId = newRoute.params.messageId as string | undefined;
            const oldMessageId = oldRoute.params.messageId as string | undefined;
            
            this.suppressRouteUpdate = true;
            
            // Handle mailbox change
            if (newMailbox !== oldMailbox) {
                const decodedMailbox = newMailbox ? decodeURIComponent(newMailbox) : null;
                
                // Only update if the mailbox actually exists and is different from current selection
                if (decodedMailbox && this.availableMailboxes) {
                    const mailbox = this.availableMailboxes.find(m => m.name === decodedMailbox);
                    if (mailbox && mailbox.name !== this.selectedMailbox) {
                        this.selectedMailbox = mailbox.name;
                    }
                }
            }
            
            // Handle message selection change
            if (newMessageId !== oldMessageId) {
                if (newMessageId) {
                    // Select the message if it exists in current list
                    const message = this.messages.find(m => m.id === newMessageId);
                    if (message && message.id !== this.selectedmessage?.id) {
                        this.selectMessage(message);
                    }
                } else if (this.selectedmessage) {
                    // Clear selection
                    this.handleCurrentChange(null);
                }
            }
            
            // Handle query parameter changes (filters)
            const query = newRoute.query;
            const oldQuery = oldRoute.query;
            
            if (JSON.stringify(query) !== JSON.stringify(oldQuery)) {
                let needsRefresh = false;
                
                // Update search term
                const newSearch = (query.search as string) ?? "";
                if (newSearch !== this.searchTerm) {
                    this.searchTerm = newSearch;
                    needsRefresh = true;
                }
                
                // Update folder
                const newFolder = (query.folder as string) ?? "INBOX";
                if (newFolder !== this.selectedFolder) {
                    this.selectedFolder = newFolder;
                    needsRefresh = true;
                }
                
                // Update sort column
                const newSort = (query.sort as string) ?? "receivedDate";
                if (newSort !== this.selectedSortColumn) {
                    this.selectedSortColumn = newSort;
                    needsRefresh = true;
                }
                
                // Update sort order
                const newOrder = query.order === 'asc' ? false : true;
                if (newOrder !== this.selectedSortDescending) {
                    this.selectedSortDescending = newOrder;
                    needsRefresh = true;
                }
                
                if (needsRefresh && !this.isInitialLoad) {
                    this.refresh(false);
                }
            }
            
            this.suppressRouteUpdate = false;
        }

        @Watch("connection")
        async onConnectionChanged() {
            if (this.connection) {
                this.connection.on("messageschanged", async () => {
                    await this.refresh(true, true);
                });
                this.connection.onServerChanged(async () => {
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
