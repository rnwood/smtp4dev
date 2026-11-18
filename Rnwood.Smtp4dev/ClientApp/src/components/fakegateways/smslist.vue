<template>
    <div class="fakegatewaylist smslist" data-testid="sms-list">
        <div class="toolbar">
            <el-input v-model="filter"
                      class="filterinput"
                      placeholder="Filter by number or text"
                      clearable
                      aria-label="Filter SMS"
                      data-testid="sms-filter" />
            <el-button-group class="toolbarbuttons">
                <el-button icon="refresh"
                           v-on:click="refresh()"
                           :disabled="loading"
                           aria-label="Refresh"
                           title="Refresh"></el-button>
                <el-button icon="Delete"
                           title="Delete all"
                           data-testid="sms-delete-all"
                           :disabled="smsList.length === 0"
                           @click="clear">Delete all</el-button>
            </el-button-group>
        </div>

        <el-alert v-if="error" type="error" title="Error" show-icon>
            {{ error.message }}
            <el-button v-on:click="refresh()">Retry</el-button>
        </el-alert>
        <el-table :data="filteredSms"
                  v-loading="loading"
                  :empty-text="emptyText"
                  highlight-current-row
                  @current-change="onTableCurrentChange"
                  class="table"
                  row-key="id"
                  stripe
                  ref="table">
            <el-table-column label="" width="40">
                <template #default="scope">
                    <el-icon v-if="scope.row.status === 'ACCEPTED'" class="status-ok" title="Accepted" aria-label="Accepted"><circle-check /></el-icon>
                    <el-icon v-else class="status-refused" title="Refused" aria-label="Refused"><circle-close /></el-icon>
                </template>
            </el-table-column>
            <el-table-column label="Received" width="150">
                <template #default="scope">
                    <span :title="scope.row.receivedAt" :data-sms-id="scope.row.id">{{ formatTime(scope.row.receivedAt) }}</span>
                </template>
            </el-table-column>
            <el-table-column prop="to" label="To" width="130" show-overflow-tooltip></el-table-column>
            <el-table-column label="From" width="90" show-overflow-tooltip>
                <template #default="scope">{{ scope.row.sender || "-" }}</template>
            </el-table-column>
            <el-table-column prop="text" label="Text" min-width="140" show-overflow-tooltip></el-table-column>
            <el-table-column prop="segments" label="Seg." width="50"></el-table-column>
        </el-table>
    </div>
</template>
<script lang="ts">
    import { Component, Vue, Prop, Watch, toNative } from "vue-facing-decorator";
    import { Mutex } from "async-mutex";
    import { TableInstance, ElNotification, ElMessageBox } from "element-plus";
    import FakeGatewaysController, { SmsMessage } from "../../ApiClient/FakeGatewaysController";
    import HubConnectionManager from "../../HubConnectionManager";
    import sortedArraySync from "../../sortedArraySync";
    import { formatLocalTime } from "./clipboard";

    function normalizePhone(value: string): string {
        let result = value.replace(/\s+/g, "");
        if (result.startsWith("00")) {
            result = "+" + result.substring(2);
        }
        return result;
    }

    @Component({})
    class SmsList extends Vue {

        @Prop({ default: null })
        connection: HubConnectionManager | null = null;

        smsList: SmsMessage[] = [];
        filter: string = "";
        error: Error | null = null;
        selectedSms: SmsMessage | null = null;
        loading: boolean = true;
        private mutex = new Mutex();
        private pendingIdToSelect: string | null = null;
        private connectionHandlersRegistered = false;

        get filteredSms(): SmsMessage[] {
            const needle = this.filter.trim().toLowerCase();
            if (!needle) {
                return this.smsList;
            }
            const phoneNeedle = normalizePhone(needle);
            return this.smsList.filter(s =>
                normalizePhone(s.to).toLowerCase().includes(phoneNeedle) ||
                s.to.toLowerCase().includes(needle) ||
                s.text.toLowerCase().includes(needle));
        }

        get emptyText(): string {
            return this.smsList.length > 0 && this.filter.trim() ? "No SMS match the filter" : "No SMS";
        }

        formatTime(iso: string): string {
            return formatLocalTime(iso);
        }

        // routeDriven: the selection changed because of the URL, so the URL must not be written back.
        handleCurrentChange(sms: SmsMessage | null, routeDriven: boolean = false) {
            this.selectedSms = sms;
            if (sms) {
                // A user choice overrides a deep link that has not been resolved yet
                this.pendingIdToSelect = null;
            }
            this.$emit("selected-sms-changed", sms, routeDriven);
        }

        onTableCurrentChange(row: SmsMessage | null) {
            // Already handled when the selection was set from code
            if ((row?.id ?? null) !== (this.selectedSms?.id ?? null)) {
                this.handleCurrentChange(row ?? null);
            }
        }


        selectSms(sms: SmsMessage) {
            this.handleCurrentChange(sms, true);
            (this.$refs.table as TableInstance).setCurrentRow(sms);
        }

        clearSelection() {
            if (this.selectedSms) {
                this.handleCurrentChange(null, true);
                (this.$refs.table as TableInstance).setCurrentRow(null);
            }
        }

        @Watch("filter")
        onFilterChanged() {
            // Do not keep showing the details of a row that the filter hides
            if (this.selectedSms && !this.filteredSms.some(s => s.id === this.selectedSms!.id)) {
                this.handleCurrentChange(null);
                (this.$refs.table as TableInstance).setCurrentRow(null);
            }
        }

        async clear() {
            try {
                await ElMessageBox.confirm(`Delete all ${this.smsList.length} SMS? The filter is not applied.`);
            } catch {
                return;
            }

            this.loading = true;
            try {
                await new FakeGatewaysController().deleteAllSms();
                await this.refresh(true);
            } catch (e: any) {
                ElNotification.error({
                    title: "Delete SMS Failed",
                    message: e.message,
                });
            } finally {
                this.loading = false;
            }
        }

        async refresh(silent: boolean = false) {
            const unlock = await this.mutex.acquire();

            try {
                this.error = null;
                this.loading = !silent;

                const items = await new FakeGatewaysController().getSmsList();
                sortedArraySync(items, this.smsList, (a: SmsMessage, b: SmsMessage) => a.id == b.id);

                if (this.pendingIdToSelect) {
                    const sms = this.smsList.find(s => s.id === this.pendingIdToSelect);
                    if (sms) {
                        this.pendingIdToSelect = null;
                        // The table renders the new rows on the next tick.
                        await this.$nextTick();
                        this.selectSms(sms);
                    }
                }

                if (this.selectedSms != null && !this.smsList.some(s => s.id == this.selectedSms!.id)) {
                    // Removed from the server: the user sees the list shrink, so the URL follows
                    this.handleCurrentChange(null);
                    (this.$refs.table as TableInstance).setCurrentRow(null);
                } else if (this.pendingIdToSelect && this.selectedSms != null) {
                    // The route points to an item that does not exist: don't show a stale detail
                    this.clearSelection();
                }
            } catch (e: any) {
                this.error = e;
            } finally {
                this.loading = false;
                unlock();
            }
        }

        async mounted() {
            const id = this.$route.params.id as string | undefined;
            if (this.$route.path.startsWith("/sms") && id && id.trim()) {
                this.pendingIdToSelect = id;
            }
            await this.refresh(false);
            this.registerConnectionHandlers();
        }

        @Watch("connection")
        onConnectionChanged() {
            this.registerConnectionHandlers();
        }

        // The hub has no off(): register the handlers exactly once.
        private registerConnectionHandlers() {
            if (this.connection && !this.connectionHandlersRegistered) {
                this.connectionHandlersRegistered = true;
                this.connection.on("smschanged", () => {
                    void this.refresh(true);
                });
                this.connection.addOnConnectedCallback(() => { void this.refresh(true); });
            }
        }

        @Watch("$route")
        onRouteChanged(newRoute: any) {
            if (!newRoute.path.startsWith("/sms")) {
                return;
            }

            const id = newRoute.params.id as string | undefined;
            if (id) {
                if (id !== this.selectedSms?.id) {
                    const sms = this.smsList.find(s => s.id === id);
                    if (sms) {
                        this.selectSms(sms);
                    } else {
                        this.pendingIdToSelect = id;
                        this.clearSelection();
                    }
                }
            } else {
                this.pendingIdToSelect = null;
                this.clearSelection();
            }
        }
    }

    export default toNative(SmsList)
</script>
<style>
    .fakegatewaylist .status-ok {
        color: var(--el-color-success);
    }

    .fakegatewaylist .status-refused {
        color: var(--el-color-danger);
    }

    .fakegatewaylist .toolbarbuttons {
        flex: 0 0 auto;
    }

    .fakegatewaylist {
        display: flex;
        flex-direction: column;
        gap: 6px;
    }

    .fakegatewaylist .table {
        overflow-y: auto;
        overflow-x: auto;
        flex: 1;
    }

    .fakegatewaylist .filterinput {
        flex: 1 1 140px;
        min-width: 140px;
    }
</style>
