<template>

    <div class="hfillpanel" v-loading="!html">
        <el-alert v-if="error" type="error">
            {{error.message}}

            <el-button v-on:click="loadMessage">Retry</el-button>
        </el-alert>

        <iframe class="fill" ref="htmlframe"></iframe>
    </div>
</template>
<script lang="ts">
    import { Component, Prop, Watch } from 'vue-property-decorator'
    import Vue from 'vue'
    import MessagesController from "../ApiClient/MessagesController";
    import ServerController from "../ApiClient/ServerController";
    import Message from "../ApiClient/Message";
    import * as srcDoc from 'srcdoc-polyfill';
    import sanitizeHtml from 'sanitize-html';

    @Component
    export default class MessageViewPlainText extends Vue {
        constructor() {
            super();
        }

        @Prop({ default: null })
        message: Message | null | undefined;
        html: string | null = null;

        error: Error | null = null;
        loading = false;

        @Watch("message")
        async onMessageChanged(value: Message | null, oldValue: Message | null) {

            await this.loadMessage();

        }

        @Watch("html")
        async onHtmlChanged(value: string) {
            this.updateIframe();
        }

        private updateIframe() {
          
            srcDoc.set(<HTMLIFrameElement>this.$refs.htmlframe, this.html ?? "");
        }
        async loadMessage() {

            this.error = null;
            this.loading = true;
            this.html = null;
           
            try {
                if (this.message != null) {

                    this.html = "<pre>" + await new MessagesController().getMessagePlainText(this.message.id) + "</pre>"
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
</script>