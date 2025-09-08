<template>

    <div class="vfillpanel" v-loading="loading">

        <el-alert v-if="error" type="error">
            {{error.message}}

            <el-button v-on:click="loadMessage">Retry</el-button>
        </el-alert>

        <div v-if="message && !message.hasHtmlBody" class="fill nodetails centrecontents">
            <div>Message has no HTML body</div>
        </div>

        <div v-if="message?.hasHtmlBody && isHtmlCompatibilityCheckDisabled" class="fill nodetails centrecontents">
            <div>HTML compatibility check is disabled</div>
        </div>

        <el-table class="fill table" stripe :data="warnings" v-if="message?.hasHtmlBody && !isHtmlCompatibilityCheckDisabled" empty-text="There are no warnings">
            <el-table-column prop="feature" label="Feature" width="180">
                <template #default="scope">
                    <span style="font-family: Courier New, Courier, monospace">{{scope.row.feature}}</span>
                </template>
            </el-table-column>
            <el-table-column prop="type" label="Type" width="180">
                <template #default="scope">
                    <a v-if="scope.row.url" target="_blank" :href="scope.row.url">{{scope.row.type}}</a>
                    <span v-if="!scope.row.url">{{scope.row.type}}</span>
                </template>
            </el-table-column>
            <el-table-column prop="browser" label="Browsers">
                <template #default="scope">
                    <div style="display: flex; flex-wrap: wrap; gap: 5px;">
                        <el-tag v-for="browser in scope.row.browsers" :type="scope.row.isError ? 'danger' : 'warning'" :key="browser">{{browser}}</el-tag>
                    </div>
                </template>
            </el-table-column>
        </el-table>
    </div>


</template>
<script lang="ts">
    import { Component, Vue, Prop, Watch, toNative, Emit } from 'vue-facing-decorator'

    import MessagesController from "../ApiClient/MessagesController";
    import Message from "../ApiClient/Message";
    import HubConnectionManager from "../ApiClient/HubConnectionManager";
    import { HtmlCompatibilityWorkerManager, type CompatibilityWarning } from "../workers/HtmlCompatibilityWorkerManager";

    @Component
    class MessageClientAnalysis extends Vue {

        @Prop({ default: null })
        message: Message | null | undefined;

        @Prop({ default: null })
        connection: HubConnectionManager | null = null;

        error: Error | null = null;
        loading = false;
        isHtmlCompatibilityCheckDisabled = false;
        private workerManager = new HtmlCompatibilityWorkerManager();

        warnings: CompatibilityWarning[] = [];

        @Watch("message")
        async onMessageChanged(value: Message | null, oldValue: Message | null) {

            await this.loadMessage();
        }

        @Watch("connection")
        onConnectionChanged() {
            if (this.connection) {
                this.connection.onServerChanged( async () => {
                    await this.loadMessage();
                });
            }
        }

        @Watch("warnings")
        onWarningsChanged() {
            this.fireWarningCountChanged()
        }

        @Emit("warning-count-changed")
        fireWarningCountChanged() {
            return this.warnings?.length ?? 0;
        }

        async loadMessage() {

            this.warnings = [];
            this.error = null;
            this.loading = true;

            try {
                if (this.message != null && this.message.hasHtmlBody && this.connection) {
                    const server = await this.connection.getServer();
                    this.isHtmlCompatibilityCheckDisabled = server.disableHtmlCompatibilityCheck;
                    
                    if (!this.isHtmlCompatibilityCheckDisabled) {
                        const html = await new MessagesController().getMessageHtml(this.message.id);
                        
                        // Use web worker for compatibility checking
                        const compatibilityResults = await this.workerManager.checkCompatibility(html);
                        this.warnings = compatibilityResults;
                    }
                }
            } catch (e: any) {
                this.error = e;
            } finally {
                this.loading = false;
            }
        }

        async created() {

            this.loadMessage();
        }

        async destroyed() {
            this.workerManager.destroy();
        }

    }

    export default toNative(MessageClientAnalysis)
</script>