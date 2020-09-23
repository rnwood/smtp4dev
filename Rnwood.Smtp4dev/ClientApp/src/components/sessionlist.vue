<template>
    <div class="sessionlist">
        <div class="toolbar">
            
                <confirmationdialog v-on:confirm="clear"
                                    always-key="deleteAllSessions"
                                    message="Are you sure you want to delete all sessions?">
                    <el-button icon="el-icon-close" title="Clear"></el-button>
                </confirmationdialog>
                <el-button
                           icon="el-icon-delete"
                           v-on:click="deleteSelected"
                           :disabled="!selectedsession" title="Delete"></el-button>
                <el-button icon="el-icon-refresh"
                           v-on:click="refresh"
                           :disabled="loading" title="Refresh"></el-button>
        </div>

        <el-alert v-if="error" type="error" title="Error" show-icon>
            {{error.message}}
            <el-button v-on:click="refresh">Retry</el-button>
        </el-alert>
        <el-table :data="sessions"
                  v-loading="loading"
                  empty-text="No sessions"
                  highlight-current-row
                  @current-change="handleCurrentChange"
                  class="table"
                  :default-sort="{prop: 'endDate', order: 'descending'}"
                  type="selection"
                  reserve-selection="true"
                  row-key="id"
                  stripe
                  ref="table">
            <el-table-column property="endDate"
                             label="End Date"
                             width="120"
                             sortable
                             :formatter="formatDate"></el-table-column>
            <el-table-column property="clientAddress" label="Client Address" sortable></el-table-column>
            <el-table-column property="numberOfMessages" label="# Msgs" width="120" sortable>
                <template slot-scope="scope">
                    <i class="el-icon-warning"
                       v-if="scope.row.terminatedWithError"
                       title="This session terminated abnormally"></i>
                    {{scope.row.numberOfMessages}}
                </template>
            </el-table-column>
        </el-table>
    </div>
</template>
<script lang="ts">
    import { Component, Prop, Watch } from "vue-property-decorator";
    import Vue from "vue";
    import SessionsController from "../ApiClient/SessionsController";
    import SessionSummary from "../ApiClient/SessionSummary";
    import * as moment from "moment";
    import HubConnectionManager from "../HubConnectionManager";
    import sortedArraySync from "../sortedArraySync";
    import { Mutex } from "async-mutex";
    import ConfirmationDialog from "@/components/confirmationdialog.vue";
    import { ElTable } from "element-ui/types/table";

    @Component({
        components: {
            confirmationdialog: ConfirmationDialog
        }
    })
    export default class SessionList extends Vue {
        constructor() {
            super();
        }


        @Prop({default: null})
        connection: HubConnectionManager | null = null;

        sessions: SessionSummary[] = [];
        error: Error | null = null;
        selectedsession: SessionSummary | null = null;
        loading: boolean = true;
        private mutex = new Mutex();

        handleCurrentChange(session: SessionSummary | null): void {
            this.selectedsession = session;
            this.$emit("selected-session-changed", session);
        }

        formatDate(
            row: number,
            column: number,
            cellValue: Date,
            index: number
        ): string {
            return (<any>moment)(cellValue).format("YYYY-MM-DD HH:mm:ss");
        }

        selectSession(session: SessionSummary) {
            (<ElTable>this.$refs.table).setCurrentRow(session);
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
            } catch (e) {
                this.$notify.error({ title: "Delete Session Failed", message: e.message });
            } finally {
                this.loading = false;
            }
        }

        async clear() {
            this.loading = true;
            try {
                await new SessionsController().deleteAll();
                this.refresh();
            } catch (e) {
                this.$notify.error({ title: "Clear Sessions Failed", message: e.message });
            } finally {
                this.loading = false;
            }
        }

        async refresh(silent: boolean=false) {
            var unlock = await this.mutex.acquire();

            try {
                this.error = null;
                this.loading = !silent;

                var newSessions = await new SessionsController().getSummaries();
                sortedArraySync(
                    newSessions,
                    this.sessions,
                    (a: SessionSummary, b: SessionSummary) => a.id == b.id
                );

                if (
                    !this.sessions.some(
                        m => this.selectedsession != null && m.id == this.selectedsession.id
                    )
                ) {
                    this.handleCurrentChange(null);
                }
            } catch (e) {
                this.error = e;
            } finally {
                this.loading = false;
                unlock();
            }
        }

        async mounted() {
            this.refresh(false);
        }
        
        @Watch("connection")
        async onConnectionChanged() {
            if (this.connection) {
                this.connection.on("sessionschanged", () => {
                    this.refresh(true);
                });
                this.connection.addOnConnectedCallback(() => this.refresh(true));
            }
        }
    }
</script>