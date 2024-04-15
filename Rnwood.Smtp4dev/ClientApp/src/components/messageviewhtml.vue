<template>

    <div class="hfillpanel" v-loading="!html">
        <el-alert v-if="error" type="error">
            {{error.message}}

            <el-button v-on:click="loadMessage">Retry</el-button>
        </el-alert>
        <el-alert v-if="wasSanitized" type="warning">
            Message HTML was sanizited for display. <el-button type="danger" size="small" v-on:click="disableSanitization">Disable (DANGER!)</el-button>
        </el-alert>

        <iframe class="fill" @load="onHtmlFrameLoaded" ref="htmlframe"></iframe>
    </div>
</template>
<script lang="ts">
    import { Component, Vue, Prop, Watch, toNative } from 'vue-facing-decorator'
    
    import MessagesController from "../ApiClient/MessagesController";
    import ServerController from "../ApiClient/ServerController";
    import Message from "../ApiClient/Message";
    import * as srcDoc from 'srcdoc-polyfill';
    import sanitizeHtml from 'sanitize-html';

    @Component
    class MessageViewHtml extends Vue {
        @Prop({ default: null })
        message: Message | null | undefined;
        html: string | null = null;
        enableSanitization = true;
        sanitizedHtml: string | null = null;
        wasSanitized: boolean = false;


        error: Error | null = null;
        loading = false;

        @Watch("message")
        async onMessageChanged(value: Message | null, oldValue: Message | null) {

            this.html = "";
            await this.loadMessage();

        }

        @Watch("html")
        async onHtmlChanged(value: string) {
            this.updateIframe();
        }

        private updateIframe() {
            this.wasSanitized = false;
            this.sanitizedHtml = "";

            if (this.html) {
                if (!this.enableSanitization) {
                    this.sanitizedHtml = this.html;
                } else {
                    this.sanitizedHtml = sanitizeHtml(this.html, { allowedTags: sanitizeHtml.defaults.allowedTags.concat("img"), allowedSchemesByTag: { "img": ["cid", "data"] } });
                    let normalizedOriginalHtml = sanitizeHtml(this.html, { allowedAttributes: false, allowedTags: false });
                    this.wasSanitized = normalizedOriginalHtml !== this.sanitizedHtml;
                }
            }

            srcDoc.set(this.$refs.htmlframe as HTMLIFrameElement, this.sanitizedHtml);
        }

        async onHtmlFrameLoaded() {
            var doc = (this.$refs.htmlframe as HTMLIFrameElement).contentDocument;
            if (!doc) {
                return;
            }

            var baseElement = doc.body.querySelector("base") || doc.createElement("base");
            baseElement.setAttribute("target", "_blank");

            doc.body.appendChild(baseElement);
        }

        async loadMessage() {

            this.error = null;
            this.loading = true;
            this.html = null;
            this.wasSanitized = false;

            this.enableSanitization = !(await new ServerController().getServer()).disableMessageSanitisation;


            try {
                if (this.message != null) {

                    this.html = await new MessagesController().getMessageHtml(this.message.id);
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

        disableSanitization() {
            this.enableSanitization = false;
            this.updateIframe();
        }
    }

    export default toNative(MessageViewHtml)
</script>