<template>
    <div class="serverlog fill vfillpanel">
        <div class="toolbar">
            <el-button size="small" @click="clearLogs">Clear</el-button>
            <el-button size="small" @click="refresh">Refresh</el-button>
            <el-checkbox v-model="autoScroll" size="small" style="margin-left: 10px;">Auto-scroll</el-checkbox>
        </div>

        <div v-loading.body="loading" class="vfillpanel fill">
            <el-alert v-if="error" type="error">
                {{error.message}}
                <el-button v-on:click="refresh">Retry</el-button>
            </el-alert>

            <div class="log-container fill">
                <pre ref="logContent" class="log-content">{{ logs }}</pre>
            </div>
        </div>
    </div>
</template>

<script lang="ts">
    import { Component, Vue, Prop, Watch, toNative } from "vue-facing-decorator";
    import ServerLogController from "../ApiClient/ServerLogController";
    import HubConnectionManager from "@/HubConnectionManager";

    @Component({
        components: {}
    })
    class ServerLog extends Vue {
        @Prop({})
        connection: HubConnectionManager | null = null;

        logs: string = "";
        error: Error | null = null;
        loading = false;
        autoScroll = true;

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

        onServerLogReceived(logEntry: string) {
            this.logs += logEntry;
            
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

        async refresh() {
            this.error = null;
            this.loading = true;

            try {
                this.logs = await new ServerLogController().getServerLog();
                
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
                this.logs = "";
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
