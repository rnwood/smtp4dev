<template>
    <div class="sessionlist">
        <div class="toolbar">
            <el-button icon="Delete"
                       v-on:click="deleteSelected"
                       :disabled="!selectedsession"
                       title="Delete">Delete</el-button>
            <el-button-group>
                <el-button icon="refresh"
                           v-on:click="refresh"
                           :disabled="loading"
                           title="Refresh"></el-button>
                <el-button icon="close" title="Clear" @click="clear"></el-button>
            </el-button-group>

        </div>

        <el-alert v-if="error" type="error" title="Error" show-icon>
            {{ error.message }}
            <el-button v-on:click="refresh">Retry</el-button>
        </el-alert>
        <el-table :data="sessions"
                  v-loading="loading"
                  empty-text="No sessions"
                  highlight-current-row
                  @current-change="handleCurrentChange"
                  class="table"
                  :default-sort="{ prop: 'endDate', order: 'descending' }"
                  type="selection"
                  reserve-selection="true"
                  row-key="id"
                  stripe
                  ref="table">
            <el-table-column property="endDate"
                             label="End Date"
                             width="180"
                             sortable
                             :formatter="formatDate"></el-table-column>
            <el-table-column property="clientAddress"
                             label="Client Address"
                             sortable></el-table-column>
            <el-table-column property="numberOfMessages"
                             label="# Msgs"
                             width="120"
                             sortable>
                <template #default="scope">
                    <el-icon title="This session terminated abnormally">
                        <warning v-if="scope.row.terminatedWithError" />
                    </el-icon>
                    {{ scope.row.numberOfMessages }}
                </template>
            </el-table-column>
            <el-table-column property="hasWarnings"
                             label=""
                             width="28">
                <template #default="scope">
                    <el-tooltip effect="light"
                                content="Session has warnings"
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
    import { Component, Vue, Prop, Watch, toNative, Emit } from "vue-facing-decorator";
    import SessionsController from "../ApiClient/SessionsController";
    import SessionSummary from "../ApiClient/SessionSummary";
    import HubConnectionManager from "../HubConnectionManager";
    import sortedArraySync from "../sortedArraySync";
    import { Mutex } from "async-mutex";
    import { TableInstance, ElNotification, ElMessageBox } from "element-plus";
    import PagedResult, { EmptyPagedResult } from "@/ApiClient/PagedResult";
    import Messagelistpager from "@/components/messagelistpager.vue";
    import ClientSettingsManager from "@/ApiClient/ClientSettingsManager";

    @Component({
        components: {
            Messagelistpager
        },
    })
    class SessionList extends Vue {

        page: number = 1;
        public pagedServerMessages: PagedResult<SessionSummary> | undefined = EmptyPagedResult<SessionSummary>();

        @Prop({ default: null })
        connection: HubConnectionManager | null = null;

        sessions: SessionSummary[] = [];
        error: Error | null = null;
        selectedsession: SessionSummary | null = null;
        loading: boolean = true;
        private mutex = new Mutex();
        private isInitialLoad = true;

        @Emit("selected-session-changed")
        handleCurrentChange(session: SessionSummary | null) {
            this.selectedsession = session;
            return session;
        }

        async handlePaginationCurrentChange(page: number) {
            this.page = page;
            await this.refresh();
        }

        async handlePaginationPageSizeChange(pageSize: number) {
            ClientSettingsManager.updateClientSettings({ pageSize });
            await this.refresh();
        }

        formatDate(
            row: number,
            column: number,
            cellValue: Date,
            index: number
        ): string {
            return cellValue?.toLocaleString();
        }

        selectSession(session: SessionSummary) {
            (this.$refs.table as TableInstance).setCurrentRow(session);
            this.handleCurrentChange(session);
        }

        async deleteSelected() {
            if (this.selectedsession == null) {
                return;
            }

            this.loading = true;
            let sessionToDelete = this.selectedsession;

            let nextIndex = this.sessions.indexOf(sessionToDelete) - 1;
            if (nextIndex >= 0) {
                this.selectSession(this.sessions[nextIndex]);
            }

            try {
                await new SessionsController().delete(sessionToDelete.id);
                this.refresh();
            } catch (e: any) {
                ElNotification.error({
                    title: "Delete Session Failed",
                    message: e.message,
                });
            } finally {
                this.loading = false;
            }
        }

        async clear() {
            try {
                await ElMessageBox.confirm("Delete all sessions?")
            } catch {
                return;
            }
            
            this.loading = true;
            try {
                await new SessionsController().deleteAll();
                this.refresh();
            } catch (e: any) {
                ElNotification.error({
                    title: "Clear Sessions Failed",
                    message: e.message,
                });
            } finally {
                this.loading = false;
            }
        }

        async refresh(silent: boolean = false) {
            var unlock = await this.mutex.acquire();

            try {
                this.error = null;
                this.loading = !silent;

                // Get current pageSize from settings
                const clientSettings = await ClientSettingsManager.getClientSettings();
                const pageSize = clientSettings.pageSize || 25;

                this.pagedServerMessages = await new SessionsController().getSummaries(
                    this.page,
                    pageSize);
                sortedArraySync(
                    this.pagedServerMessages.results,
                    this.sessions,
                    (a: SessionSummary, b: SessionSummary) => a.id == b.id
                );

                // On initial load, check if there's a session ID in the route
                if (this.isInitialLoad) {
                    const sessionId = this.$route.params.sessionId as string | undefined;
                    if (sessionId) {
                        const session = this.sessions.find(s => s.id === sessionId);
                        if (session) {
                            this.selectSession(session);
                        }
                    }
                }

                if (
                    !this.sessions.some(
                        (m) => this.selectedsession != null && m.id == this.selectedsession.id
                    )
                ) {
                    this.handleCurrentChange(null);
                }
            } catch (e: any) {
                this.error = e;
            } finally {
                this.loading = false;
                unlock();
            }
        }

        async mounted() {
            await this.refresh(false);
            this.isInitialLoad = false;
        }

        async created() {
        }

        @Watch("connection")
        async onConnectionChanged() {
            if (this.connection) {
                this.connection.on("sessionschanged", () => {
                    this.refresh(true);
                });
                this.connection.on("sessionupdated", (sessionId: string) => {
                    this.refresh(true);
                });
                this.connection.addOnConnectedCallback(() => this.refresh(true));
            }
        }

        @Watch("$route")
        onRouteChanged(newRoute: any, oldRoute: any) {
            // Handle route changes for session selection (e.g., browser back/forward)
            const newSessionId = newRoute.params.sessionId as string | undefined;
            const oldSessionId = oldRoute.params.sessionId as string | undefined;
            
            if (newSessionId !== oldSessionId && !this.isInitialLoad) {
                if (newSessionId) {
                    const session = this.sessions.find(s => s.id === newSessionId);
                    if (session && session.id !== this.selectedsession?.id) {
                        this.selectSession(session);
                    }
                } else {
                    // Route changed to sessions root, clear selection
                    this.handleCurrentChange(null);
                }
            }
        }
    }

    export default toNative(SessionList)
</script>