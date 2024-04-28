<template>

    <div class="vfillpanel" v-loading="loading">

        <el-alert v-if="error" type="error">
            {{error.message}}

            <el-button v-on:click="loadMessage">Retry</el-button>
        </el-alert>

        <div v-if="message && !message.hasHtmlBody" class="fill nodetails centrecontents">
            <div>Message has no HTML body</div>
        </div>

        <el-table class="fill table" stripe :data="warnings" v-if="message?.hasHtmlBody" empty-text="There are no warnings">
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
    </div>


</template>
<script lang="ts">
    import { Component, Vue, Prop, Watch, toNative, Emit } from 'vue-facing-decorator'

    import MessagesController from "../ApiClient/MessagesController";
    import Message from "../ApiClient/Message";
    import { HtmlValidate, Message as HtmlValidateMessage } from "html-validate";

    @Component
    class MessageHtmlValidation extends Vue {

        @Prop({ default: null })
        message: Message | null | undefined;

        error: Error | null = null;
        loading = false;
        html = "";

        warnings: HtmlValidateMessage[] = [];

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



        async loadMessage() {

            this.warnings = [];
            this.error = null;
            this.loading = true;
            this.html = "";

            try {
                const newWarnings = [];
                if (this.message != null && this.message.hasHtmlBody) {

                    this.html = await new MessagesController().getMessageHtml(this.message.id);

                    const report = await new HtmlValidate().validateString(this.html, "messagebody");
                    for (const r of report.results) {
                        newWarnings.push(...r.messages);
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