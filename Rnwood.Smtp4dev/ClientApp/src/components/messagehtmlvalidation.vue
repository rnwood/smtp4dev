<template>

    <div class="vfillpanel" v-loading="loading">

        <el-alert v-if="error" type="error">
            {{error.message}}

            <el-button v-on:click="loadMessage">Retry</el-button>
        </el-alert>

        <div v-if="message && !message.hasHtmlBody" class="fill nodetails centrecontents">
            <div>Message has no HTML body</div>
        </div>

        <div v-if="message?.hasHtmlBody && isHtmlValidationDisabled" class="fill nodetails centrecontents">
            <div>HTML validation is disabled</div>
        </div>

        <div v-if="message?.hasHtmlBody && !isHtmlValidationDisabled" class="vfillpanel">
            <el-table class="table" stripe :data="paginatedWarnings" empty-text="There are no warnings">
                <el-table-column prop="message" label="Message" width="200">
                    <template #default="scope">
                        <a target="_blank" :href="scope.row.ruleUrl">{{scope.row.message}}</a>
                    </template>
                </el-table-column>
                <el-table-column width="50" Label="Loc">
                    <template #default="scope">
                        {{scope.row.line}}:{{scope.row.column}}
                        </template>
                </el-table-column>
                <el-table-column prop="line" label="Source">
                    <template #default="scope">
                          <code style="display: block; white-space:pre; font-size: 9pt; height: 100%; width: 100%; overflow: auto;">
                            {{this.html.split("\n")[scope.row.line-1]}}{{"\n"}}
                            {{" ".repeat(Math.max(0, scope.row.column-2))}}{{"^".repeat(scope.row.size)}}
                        </code>

                    </template>
                </el-table-column>

            </el-table>
            
            <el-pagination
                v-if="warnings.length > pageSize"
                :current-page="currentPage"
                :page-size="pageSize"
                :page-sizes="[10, 25, 50, 100]"
                :total="warnings.length"
                layout="total, sizes, prev, pager, next, jumper"
                @size-change="handleSizeChange"
                @current-change="handleCurrentChange"
                style="margin-top: 10px; text-align: center;"
            />
        </div>
    </div>


</template>
<script lang="ts">
    import { Component, Vue, Prop, Watch, toNative, Emit } from 'vue-facing-decorator'

    import MessagesController from "../ApiClient/MessagesController";
    import Message from "../ApiClient/Message";
    import { HtmlValidate, Message as HtmlValidateMessage } from "html-validate";
    import HubConnectionManager from "../ApiClient/HubConnectionManager";

    @Component
    class MessageHtmlValidation extends Vue {

        @Prop({ default: null })
        message: Message | null | undefined;

        error: Error | null = null;
        loading = false;
        html = "";
        
        warnings: HtmlValidateMessage[] = [];
        currentPage = 1;
        pageSize = 25;
        isHtmlValidationDisabled = false;


        @Prop({ default: null })
        connection: HubConnectionManager | null = null;


        @Watch("connection")
        onConnectionChanged() {
            if (this.connection) {
                this.connection.onServerChanged( async () => {
                    await this.refresh();
                });

                this.connection.addOnConnectedCallback(() => this.refresh());
            }
        }

        @Watch("message")
        async onMessageChanged(value: Message | null, oldValue: Message | null) {

            await this.loadMessage();
        }

        @Watch("warnings")
        onWarningsChanged() {
            this.fireWarningCountChanged()
        }

        @Emit("warning-count-changed")
        fireWarningCountChanged() {
            return this.warnings?.length ?? 0;
        }

        get paginatedWarnings() {
            const start = (this.currentPage - 1) * this.pageSize;
            const end = start + this.pageSize;
            return this.warnings.slice(start, end);
        }

        handleSizeChange(newSize: number) {
            this.pageSize = newSize;
            this.currentPage = 1;
        }

        handleCurrentChange(newPage: number) {
            this.currentPage = newPage;
        }

        async refresh() {
            if (this.connection) {
                await this.loadMessage();
            }
        }



        async loadMessage() {

            this.warnings = [];
            this.error = null;
            this.loading = true;
            this.html = "";
            this.currentPage = 1;

            try {
                const newWarnings = [];
                if (this.message != null && this.message.hasHtmlBody && this.connection) {
                    const server = await this.connection.getServer();
                    this.isHtmlValidationDisabled = server.disableHtmlValidation;
                    
                    if (!this.isHtmlValidationDisabled) {
                        this.html = await new MessagesController().getMessageHtml(this.message.id);
                        const config = JSON.parse(server.htmlValidateConfig);

                        const report = await new HtmlValidate(config).validateString(this.html, "messagebody");
                        for (const r of report.results) {
                            newWarnings.push(...r.messages);
                        }
                    }
                }
                this.warnings = newWarnings;
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

        }

    }

    export default toNative(MessageHtmlValidation)
</script>