<template>
    <div class="hfillpanel" v-loading="loading">
        <el-alert v-if="error" type="error">
            {{error.message}}

            <el-button v-on:click="loadMessage">Retry</el-button>
        </el-alert>

        <div class="toolbar"><el-button size="small" @click="download">Open</el-button></div>
        <div v-show="source" class="vfillpanel fill">
            <textview :text="source" class="fill" :lang="sourceLang"></textview>
        </div>
    </div>
</template>
<script lang="ts">
    import { Component, Vue, Prop, Watch, toNative } from 'vue-facing-decorator'
    
    import MessagesController from "../ApiClient/MessagesController";
    import Message from "../ApiClient/Message";
    import TextView from "@/components/textview.vue";

    @Component({
        components: {
            textview: TextView
        }
    })
    class MessageSource extends Vue {

        @Prop()
        message: Message | null = null;

        source: string | null = null;
        sourceurl: string | null = null;
        sourceLang: string = "text";
        error: Error | null = null;
        loading = false;

        @Prop({ default: "source" })
        type!: "source" | "raw";

        @Watch("message")
        async onMessageChanged(value: Message, oldValue: Message) {

            await this.loadMessage();

        }

        download() {
            if (this.sourceurl) {
                window.open(this.sourceurl);
            }
        }

        async loadMessage() {

            this.error = null;
            this.loading = true;
            this.source = null;
            this.sourceurl = null;
            this.sourceLang = "text";

            try {
                if (this.message != null) {
                    if (this.type === "raw") {
                        this.sourceurl = new MessagesController().getMessageSourceRaw_url(this.message.id);
                        this.source = await new MessagesController().getMessageSourceRaw(this.message.id);

                    } else {
                        this.sourceurl = new MessagesController().getMessageSource_url(this.message.id);
                        this.source = await new MessagesController().getMessageSource(this.message.id);
                        this.sourceLang = this.message.hasHtmlBody ? "html" : "text";
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

        }
    }

    export default toNative(MessageSource);
</script>