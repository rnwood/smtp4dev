<template>
    <div class="serverlog fill vfillpanel">
        <div class="toolbar">
            <el-button size="small" @click="clearLogs">Clear</el-button>
            <el-button size="small" @click="refresh">Refresh</el-button>
            <el-checkbox v-model="autoScroll" size="small" style="margin-left: 10px;">Auto-scroll</el-checkbox>
            
            <el-divider direction="vertical" />
            
            <el-select v-model="selectedLevel" placeholder="All Levels" size="small" clearable @change="applyFilters" style="width: 150px;">
                <el-option label="All Levels" :value="null" />
                <el-option v-for="level in availableLevels" :key="level" :label="level" :value="level" />
            </el-select>
            
            <el-select v-model="selectedSource" placeholder="All Sources" size="small" clearable @change="applyFilters" style="width: 200px; margin-left: 10px;">
                <el-option label="All Sources" :value="null" />
                <el-option v-for="source in availableSources" :key="source" :label="source" :value="source" />
            </el-select>
            
            <el-input 
                v-model="searchText" 
                placeholder="Search logs..." 
                size="small" 
                clearable 
                @input="onSearchInput"
                style="width: 250px; margin-left: 10px;">
                <template #prefix>
                    <el-icon><search /></el-icon>
                </template>
            </el-input>
        </div>

        <div v-loading.body="loading" class="vfillpanel fill">
            <el-alert v-if="error" type="error">
                {{error.message}}
                <el-button v-on:click="refresh">Retry</el-button>
            </el-alert>

            <div class="log-container fill">
                <pre ref="logContent" class="log-content">{{ displayedLogs }}</pre>
            </div>
        </div>
    </div>
</template>

<script lang="ts">
    import { Component, Vue, Prop, toNative } from "vue-facing-decorator";
    import ServerLogController from "../ApiClient/ServerLogController";
    import LogEntry from "../ApiClient/LogEntry";
    import HubConnectionManager from "@/HubConnectionManager";

    @Component({
        components: {}
    })
    class ServerLog extends Vue {
        @Prop({})
        connection: HubConnectionManager | null = null;

        logEntries: LogEntry[] = [];
        availableLevels: string[] = [];
        availableSources: string[] = [];
        selectedLevel: string | null = null;
        selectedSource: string | null = null;
        searchText: string = "";
        error: Error | null = null;
        loading = false;
        autoScroll = true;
        searchDebounceTimer: number | null = null;

        get displayedLogs(): string {
            return this.logEntries.map(e => e.formattedMessage).join("");
        }

        async mounted() {
            await this.refresh();

            if (this.connection) {
                this.connection.on("serverlogreceived", this.onServerLogReceived.bind(this));
            }
        }

        beforeUnmount() {
            if (this.connection) {
                this.connection._connection.off("serverlogreceived", this.onServerLogReceived);
            }
        }

        onServerLogReceived(logEntry: LogEntry) {
            // Add new entry to the buffer
            this.logEntries.push(logEntry);
            
            // Update available sources and levels if needed
            if (!this.availableSources.includes(logEntry.source)) {
                this.availableSources.push(logEntry.source);
                this.availableSources.sort();
            }
            if (!this.availableLevels.includes(logEntry.level)) {
                this.availableLevels.push(logEntry.level);
                this.availableLevels.sort();
            }
            
            // Apply filters to new entry and update display
            this.applyFilters();
            
            if (this.autoScroll) {
                this.$nextTick(() => {
                    this.scrollToBottom();
                });
            }
        }

        scrollToBottom() {
            const logContent = this.$refs.logContent as HTMLElement;
            if (logContent) {
                logContent.scrollTop = logContent.scrollHeight;
            }
        }

        onSearchInput() {
            // Debounce search input
            if (this.searchDebounceTimer) {
                clearTimeout(this.searchDebounceTimer);
            }
            this.searchDebounceTimer = window.setTimeout(() => {
                this.applyFilters();
            }, 300);
        }

        async applyFilters() {
            this.error = null;
            this.loading = true;

            try {
                const entries = await new ServerLogController().getServerLogEntries(
                    this.selectedLevel || undefined,
                    this.selectedSource || undefined,
                    this.searchText || undefined
                );
                
                this.logEntries = entries;
                
                if (this.autoScroll) {
                    this.$nextTick(() => {
                        this.scrollToBottom();
                    });
                }
            } catch (e: any) {
                this.error = e;
            } finally {
                this.loading = false;
            }
        }

        async refresh() {
            this.error = null;
            this.loading = true;

            try {
                // Load available filter options
                const [levels, sources, entries] = await Promise.all([
                    new ServerLogController().getServerLogLevels(),
                    new ServerLogController().getServerLogSources(),
                    new ServerLogController().getServerLogEntries(
                        this.selectedLevel || undefined,
                        this.selectedSource || undefined,
                        this.searchText || undefined
                    )
                ]);
                
                this.availableLevels = levels;
                this.availableSources = sources;
                this.logEntries = entries;
                
                if (this.autoScroll) {
                    this.$nextTick(() => {
                        this.scrollToBottom();
                    });
                }
            } catch (e: any) {
                this.error = e;
            } finally {
                this.loading = false;
            }
        }

        async clearLogs() {
            this.error = null;
            this.loading = true;

            try {
                await new ServerLogController().clearServerLog();
                this.logEntries = [];
                this.availableLevels = [];
                this.availableSources = [];
                this.selectedLevel = null;
                this.selectedSource = null;
                this.searchText = "";
            } catch (e: any) {
                this.error = e;
            } finally {
                this.loading = false;
            }
        }
    }

    export default toNative(ServerLog);
</script>

<style scoped lang="scss">
    .serverlog {
        display: flex;
        flex-direction: column;
        
        .toolbar {
            padding: 10px;
            border-bottom: 1px solid var(--el-border-color);
            background-color: var(--el-bg-color);
            display: flex;
            align-items: center;
            flex-wrap: wrap;
            gap: 5px;
        }

        .log-container {
            overflow: auto;
            flex: 1;
            background-color: var(--el-bg-color);
        }

        .log-content {
            margin: 0;
            padding: 10px;
            font-family: 'Courier New', Courier, monospace;
            font-size: 12px;
            white-space: pre-wrap;
            word-wrap: break-word;
            overflow-x: auto;
            min-height: 100%;
        }
    }
</style>
