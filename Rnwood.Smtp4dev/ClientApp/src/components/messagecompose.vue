<template>
    <div style="display: flex; flex-direction: column; gap: 12px;">

        <el-form size="small">
            <el-form-item label="From">
                <el-input v-if="fromChoices.length <= 1" v-model="from" />
                <el-select v-if="fromChoices.length > 1" filterable allow-create v-model="from">
                    <el-option v-for="fromChoice in fromChoices"
                               :key="fromChoice"
                               :label="fromChoice"
                               :value="fromChoice" />
                </el-select>
            </el-form-item>
            <el-form-item label="To">
                <el-input v-model="to" />
            </el-form-item>
            <el-form-item label="CC">
                <el-input v-model="cc" />
            </el-form-item>
            <el-form-item>
                <el-checkbox v-model="deliverToAll">Deliver to all recipients</el-checkbox>
            </el-form-item>
            <el-form-item label="BCC" v-if="deliverToAll">
                <el-input v-model="bcc" />
            </el-form-item>


            <el-form-item label="Subject">
                <el-input v-model="subject" />
            </el-form-item>
        </el-form>

        <quillEditor style="height: 50vh" ref="editor"  content-type="html"></quillEditor>

        <div style="display: flex; justify-content: end;">
            <el-button @click="send" type="primary" :loading="sendInProgress">Send</el-button>
            <el-button @click="close">Cancel</el-button>
        </div>
    </div>
</template>

<script lang="ts">
    import { Component, Vue, Prop, Watch, toNative, Emit } from 'vue-facing-decorator'
    import { QuillEditor } from '@vueup/vue-quill';
    import '@vueup/vue-quill/dist/vue-quill.snow.css'
    import Message from '../ApiClient/Message';
    import MessagesController from '../ApiClient/MessagesController';
    import { ElNotification } from 'element-plus';

    @Component({ components: { quillEditor: QuillEditor } })
    class MessageCompose extends Vue {

        from = "";
        to = "";
        cc = "";
        bcc = "";
        deliverToAll = false;
        subject = "";
        sendInProgress = false;
        fromChoices: string[] = [];

        @Prop({ default: null })
        replyToMessage: Message | null = null;

        @Prop
        replyAll = false;

        mounted() {
            this.onReplyToMessageChanged();
        }

        @Emit("closed")
        async close() {
        }

        async send() {
            try {
                this.sendInProgress = true

                const body = (this.$refs.editor as any).getHTML();

                if (this.replyToMessage) {
                    await new MessagesController().reply(this.replyToMessage?.id, this.from, this.to, this.cc, this.bcc, this.deliverToAll, this.subject,body);
                } else {
                    await new MessagesController().send(this.from, this.to, this.cc, this.bcc, this.deliverToAll, this.subject, body);
                }
                
                ElNotification.success({ title: "Message sent" });


                this.close();
            } catch (e: any) {
                ElNotification.error({
                    title: "Send Failed",
                    message: e.message,
                });
            } finally {
                this.sendInProgress = false;
            }
        }

        @Watch("replyToMessage")
        @Watch("replyAll")
        async onReplyToMessageChanged() {

            let body="";

            if (this.replyToMessage) {

                this.subject = "Re: " + this.replyToMessage.subject;
                this.from = this.replyToMessage.deliveredTo[0];
                this.fromChoices = this.replyToMessage.deliveredTo;

                if (this.replyAll) {
                    this.cc = this.replyToMessage.cc.join(", ");
                    this.to = ((this.replyToMessage.headers.find(h => h.name.localeCompare("Reply-To"))?.value ?? this.replyToMessage.from) + ", " + this.replyToMessage.to.join(","));
                } else {
                    this.cc = "";
                    this.to = this.replyToMessage.headers.find(h => h.name.localeCompare("Reply-To"))?.value ?? this.replyToMessage.from;

                }

                this.bcc = "";


                let messageHtml: string;

                if (this.replyToMessage.hasHtmlBody) {
                    messageHtml = await new MessagesController().getMessageHtml(this.replyToMessage.id);
                } else {
                    const text = await new MessagesController().getMessagePlainText(this.replyToMessage.id);

                    const textArea = document.createElement("textarea");
                    textArea.innerText = text;
                    messageHtml = textArea.innerHTML.split("<br>").join("\n");
                }

                body = `<br/><br/>On ${this.replyToMessage.receivedDate} ${this.replyToMessage.from} wrote:<br/><br/><blockquote type="cite">${messageHtml}</blockquote>`;

            } else {
                this.subject =""
                this.from = "";
                this.fromChoices=[];
                this.to="";
                this.cc="";
                this.bcc="";
                body = "";
            }

            (this.$refs.editor as any).setHTML('');
            (this.$refs.editor as any).pasteHTML(body, 'silent');
        }

    }




    export default toNative(MessageCompose);
</script>