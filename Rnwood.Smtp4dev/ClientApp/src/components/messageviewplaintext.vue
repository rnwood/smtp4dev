<template>

    <div class="hfillpanel" v-loading="!html">
        <el-alert v-if="error" type="error">
            {{error.message}}

            <el-button v-on:click="loadMessage">Retry</el-button>
        </el-alert>

        <iframe class="fill plaintextview" ref="htmlframe"></iframe>
    </div>
</template>
<script lang="ts">
    import { Component, Vue, Prop, Watch, toNative } from 'vue-facing-decorator'
    
    import MessagesController from "../ApiClient/MessagesController";
    import Message from "../ApiClient/Message";
    import * as srcDoc from 'srcdoc-polyfill';

    @Component
    class MessageViewPlainText extends Vue {

        @Prop({ default: null })
        message: Message | null | undefined;
        html: string | null = null;

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
          
            srcDoc.set(this.$refs.htmlframe as HTMLIFrameElement, this.html ?? "");
        }

        private escapeHtml(text: string): string {
            const div = document.createElement('div');
            div.textContent = text;
            return div.innerHTML;
        }
        async loadMessage() {

            this.error = null;
            this.loading = true;
            this.html = null;
           
            try {
                if (this.message != null) {

                    const plainText = await new MessagesController().getMessagePlainText(this.message.id);
                    this.html = "<pre>" + this.escapeHtml(plainText) + "</pre>";
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

    export default toNative(MessageViewPlainText)
</script>