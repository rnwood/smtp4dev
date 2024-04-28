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
    import { doIUseEmail } from '@jsx-email/doiuse-email';

    @Component
    class MessageClientAnalysis extends Vue {

        @Prop({ default: null })
        message: Message | null | undefined;

        error: Error | null = null;
        loading = false;

        warnings: { message: string, feature: string, type: string, browsers: string[], url: string, isError: boolean }[] =[];

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

        private parseWarning(warning: string, isError: boolean) {

            const details = { message: warning, type: "", feature: "", browser: "", url: "", isError: false };
            const detailsMatch = warning.match(/^`(.+)` is (.+) by `(.+)`$/);

            if (detailsMatch) {
                details.feature = detailsMatch[1] ?? null;
                details.type = detailsMatch[2] ?? null;
                details.browser = detailsMatch[3] ?? null;
                details.isError = isError;

                if (details.feature.endsWith(" element")) {
                    details.url = `https://www.caniemail.com/features/html-${details.feature.replace("<", "").replace("> element", "")}/`;
                } else {
                    details.url = `https://www.caniemail.com/features/css-${details.feature.replace(":", "-")}/`;

                }
            }

            return details;
        }

        async loadMessage() {

            this.warnings = [];
            this.error = null;
            this.loading = true;

            try {
                const newWarnings = [];
                if (this.message != null && this.message.hasHtmlBody) {

                    const html = await new MessagesController().getMessageHtml(this.message.id);
                    const doIUseResults = doIUseEmail(html, { emailClients: ["*"] });

                    const allWarnings = [];
                    for (const warning of doIUseResults.warnings) {
                        const details = this.parseWarning(warning, false);
                        allWarnings.push(details);
                    }

                    if (doIUseResults.success == false) {
                        for (const warning of doIUseResults.errors) {
                            const details = this.parseWarning(warning,true);
                            allWarnings.push(details);
                        }

                    }

                    const allGrouped = Object.groupBy(allWarnings, i => i.feature + " " + i.type);
                    for (const groupKey in allGrouped) {
                        const groupItems = allGrouped[groupKey]!;
                        newWarnings.push({
                            type: groupItems[0].type, 
                            
                            feature: groupItems[0].feature,
                            message: groupItems[0].message, 
                            
                            url: groupItems[0].url,
                            browsers: groupItems.map(i => i.browser),
                            isError: groupItems[0].isError
                        })
                    }

                    this.warnings = newWarnings;

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

        }

    }

    export default toNative(MessageClientAnalysis)
</script>