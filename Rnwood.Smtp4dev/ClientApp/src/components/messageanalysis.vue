<template>

    <div>
        <h1>Analysis:</h1>

        <template v-for="i in doIUseResults?.warnings ?? []" :key="i">
            <div>
                {{i}}
            </div>
        </template>
    </div>


</template>
<script lang="ts">
    import { Component, Vue, Prop, Watch, toNative } from 'vue-facing-decorator'
    
    import MessagesController from "../ApiClient/MessagesController";
    import ServerController from "../ApiClient/ServerController";
    import Message from "../ApiClient/Message";
    import { doIUseEmail } from '@jsx-email/doiuse-email';

    @Component
    class MessageAnalysis extends Vue {

        @Prop({ default: null })
        message: Message | null | undefined;
        html: string | null = null;
        
        error: Error | null = null;
        loading = false;

        doIUseResults : ReturnType<typeof doIUseEmail> | null = null;

        @Watch("message")
        async onMessageChanged(value: Message | null, oldValue: Message | null) {

            this.html = "";
            await this.loadMessage();

        }

        @Watch("html")
        async onHtmlChanged(value: string) {

        }

        async loadMessage() {

            this.error = null;
            this.loading = true;
            this.html = null;

            try {
                if (this.message != null) {

                    this.html = await new MessagesController().getMessageHtml(this.message.id);
                    this.doIUseResults = doIUseEmail(this.html, {emailClients: ["*"]});
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

    export default toNative(MessageAnalysis)
</script>