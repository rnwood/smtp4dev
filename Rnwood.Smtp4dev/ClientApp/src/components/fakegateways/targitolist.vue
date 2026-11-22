<template>
    <div class="fakegatewaylist targitolist" data-testid="targito-list">
        <div class="toolbar">
            <el-input v-model="filter"
                      class="filterinput"
                      placeholder="Filter by e-mail or template"
                      clearable
                      aria-label="Filter Targito calls"
                      data-testid="targito-filter" />
            <el-select v-model="methodFilter" class="methodselect" aria-label="Filter by method" data-testid="targito-method-filter">
                <el-option label="All methods" value="all"></el-option>
                <el-option v-for="m in methods" :key="m" :label="m" :value="m"></el-option>
            </el-select>
            <el-button-group class="toolbarbuttons">
                <el-button icon="refresh"
                           v-on:click="refresh()"
                           :disabled="loading"
                           aria-label="Refresh"
                           title="Refresh"></el-button>
                <el-button icon="Delete"
                           title="Delete all"
                           data-testid="targito-delete-all"
                           :disabled="items.length === 0"
                           @click="clear">Delete all</el-button>
            </el-button-group>
        </div>

        <el-alert v-if="error" type="error" title="Error" show-icon>
            {{ error.message }}
            <el-button v-on:click="refresh()">Retry</el-button>
        </el-alert>
        <el-table :data="filteredItems"
                  v-loading="loading"
                  :empty-text="emptyText"
                  highlight-current-row
                  @current-change="onTableCurrentChange"
                  class="table"
                  row-key="id"
                  stripe
                  ref="table">
            <el-table-column label="Received" width="150">
                <template #default="scope">
                    <span :title="scope.row.receivedAt" :data-targito-id="scope.row.id">{{ formatTime(scope.row.receivedAt) }}</span>
                </template>
            </el-table-column>
            <el-table-column label="Method" width="130">
                <template #default="scope">
                    <el-tooltip :content="scope.row.method" placement="top" effect="light">
                        <el-tag size="small" type="info">{{ shortMethod(scope.row.method) }}</el-tag>
                    </el-tooltip>
                </template>
            </el-table-column>
            <el-table-column label="To" min-width="130" show-overflow-tooltip>
                <template #default="scope">{{ recipient(scope.row) }}</template>
            </el-table-column>
            <el-table-column label="Template" width="110" show-overflow-tooltip>
                <template #default="scope">{{ scope.row.templateName || scope.row.templateId || "-" }}</template>
            </el-table-column>
        </el-table>
    </div>
</template>
<script lang="ts">
    import { Component, Vue, Prop, Watch, toNative } from "vue-facing-decorator";
    import { Mutex } from "async-mutex";
    import { TableInstance, ElNotification, ElMessageBox } from "element-plus";
    import FakeGatewaysController, { TargitoRecord } from "../../ApiClient/FakeGatewaysController";
    import HubConnectionManager from "../../HubConnectionManager";
    import sortedArraySync from "../../sortedArraySync";
    import { formatLocalTime } from "./clipboard";

    @Component({})
    class TargitoList extends Vue {

        @Prop({ default: null })
        connection: HubConnectionManager | null = null;

        items: TargitoRecord[] = [];
        filter: string = "";
        methodFilter: string = "all";
        error: Error | null = null;
        selectedItem: TargitoRecord | null = null;
        loading: boolean = true;
        private mutex = new Mutex();
        private pendingIdToSelect: string | null = null;
        private connectionHandlersRegistered = false;

        get methods(): string[] {
            return Array.from(new Set(this.items.map(i => i.method))).sort((a, b) => a.localeCompare(b));
        }

        get filteredItems(): TargitoRecord[] {
            const needle = this.filter.trim().toLowerCase();
            return this.items.filter(i => {
                if (this.methodFilter !== "all" && i.method !== this.methodFilter) {
                    return false;
                }
                if (!needle) {
                    return true;
                }
                return [i.email, i.oldEmail, i.templateId, i.templateName]
                    .some(v => !!v && v.toLowerCase().includes(needle));
            });
        }

        get emptyText(): string {
            const filtered = this.filter.trim() !== "" || this.methodFilter !== "all";
            return this.items.length > 0 && filtered ? "No Targito calls match the filter" : "No Targito calls";
        }

        shortMethod(method: string): string {
            switch (method) {
                case "SendMailingToRecipient": return "SendMailing";
                case "ChangeEmailAddress": return "ChangeEmail";
                default: return method;
            }
        }

        recipient(item: TargitoRecord): string {
            if (item.oldEmail) {
                return `${item.oldEmail} → ${item.email ?? ""}`;
            }
            return item.email || "-";
        }

        formatTime(iso: string): string {
            return formatLocalTime(iso);
        }

        // routeDriven: the selection changed because of the URL, so the URL must not be written back.
        handleCurrentChange(item: TargitoRecord | null, routeDriven: boolean = false) {
            this.selectedItem = item;
            if (item) {
                // A user choice overrides a deep link that has not been resolved yet
                this.pendingIdToSelect = null;
            }
            this.$emit("selected-targito-changed", item, routeDriven);
        }

        onTableCurrentChange(row: TargitoRecord | null) {
            // Already handled when the selection was set from code
            if ((row?.id ?? null) !== (this.selectedItem?.id ?? null)) {
                this.handleCurrentChange(row ?? null);
            }
        }


        selectItem(item: TargitoRecord) {
            this.handleCurrentChange(item, true);
            (this.$refs.table as TableInstance).setCurrentRow(item);
        }

        clearSelection() {
            if (this.selectedItem) {
                this.handleCurrentChange(null, true);
                (this.$refs.table as TableInstance).setCurrentRow(null);
            }
        }

        hideSelectionIfFilteredOut() {
            // Do not keep showing the details of a row that the filter hides
            if (this.selectedItem && !this.filteredItems.some(s => s.id === this.selectedItem!.id)) {
                this.handleCurrentChange(null);
                (this.$refs.table as TableInstance).setCurrentRow(null);
            }
        }

        @Watch("filter")
        onFilterChanged() {
            this.hideSelectionIfFilteredOut();
        }

        @Watch("methodFilter")
        onMethodFilterChanged() {
            this.hideSelectionIfFilteredOut();
        }

        async clear() {
            try {
                await ElMessageBox.confirm(`Delete all ${this.items.length} Targito calls? The filter is not applied.`);
            } catch {
                return;
            }

            this.loading = true;
            try {
                await new FakeGatewaysController().deleteAllTargito();
                await this.refresh(true);
            } catch (e: any) {
                ElNotification.error({
                    title: "Delete Targito Failed",
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

                const items = await new FakeGatewaysController().getTargitoList();
                sortedArraySync(items, this.items, (a: TargitoRecord, b: TargitoRecord) => a.id == b.id);

                if (this.pendingIdToSelect) {
                    const item = this.items.find(s => s.id === this.pendingIdToSelect);
                    if (item) {
                        this.pendingIdToSelect = null;
                        // The table renders the new rows on the next tick.
                        await this.$nextTick();
                        this.selectItem(item);
                    }
                }

                if (this.selectedItem != null && !this.items.some(s => s.id == this.selectedItem!.id)) {
                    // Removed from the server: the user sees the list shrink, so the URL follows
                    this.handleCurrentChange(null);
                    (this.$refs.table as TableInstance).setCurrentRow(null);
                } else if (this.pendingIdToSelect && this.selectedItem != null) {
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
            if (this.$route.path.startsWith("/targito") && id && id.trim()) {
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
                this.connection.on("targitochanged", () => {
                    void this.refresh(true);
                });
                void this.connection.addOnConnectedCallback(() => { void this.refresh(true); });
            }
        }

        @Watch("$route")
        onRouteChanged(newRoute: any) {
            if (!newRoute.path.startsWith("/targito")) {
                return;
            }

            const id = newRoute.params.id as string | undefined;
            if (id) {
                if (id !== this.selectedItem?.id) {
                    const item = this.items.find(s => s.id === id);
                    if (item) {
                        this.selectItem(item);
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

    export default toNative(TargitoList)
</script>
<style>
    .fakegatewaylist .methodselect {
        flex: 0 0 150px;
        width: 150px;
    }
</style>
