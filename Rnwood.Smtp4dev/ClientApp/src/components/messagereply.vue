<template>
    <div style="display: flex; flex-direction: column; gap: 12px;">

        <el-form size="small">
            <el-form-item label="From">
                <el-input v-model="from" />
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

            <quillEditor style="height: 50vh"  v-model:value="body"></quillEditor>

        <div style="display: flex; justify-content: end;">
            <el-button @click="send" type="primary" :loading="sendInProgress">Send</el-button>
            <el-button @click="close">Cancel</el-button>
        </div>
    </div>
</template>

<script lang="ts">
    import { Component, Vue, Prop, Watch, toNative, Emit } from 'vue-facing-decorator'
    import { quillEditor } from 'vue3-quill';
    import Message from '../ApiClient/Message';
    import MessagesController from '../ApiClient/MessagesController';
    import { ElNotification } from 'element-plus';

    @Component({ components: { quillEditor: quillEditor } })
    class MessageReply extends Vue {

        body = "";
        from = "";
        to = "";
        cc = "";
        bcc = "";
        deliverToAll = false;
        subject = "";
        sendInProgress = false;

        @Prop({ default: null })
        message: Message | null = null;

        @Prop
        replyAll = false;

        mounted() {
            this.onMessageChanged();
        }

        @Emit("closed")
        async close() {
        }

        async send() {
            try {
                this.sendInProgress = true
                await new MessagesController().reply(this.message!.id, this.from, this.to, this.cc, this.bcc, this.deliverToAll, this.subject, this.body);
                ElNotification.success({ title: "Reply sent" });
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

        @Watch("message")
        @Watch("replyAll")
        async onMessageChanged() {

            if (!this.message) {
                return;
            }

            this.subject = "Re: " + this.message.subject;
            this.from = this.message.to[0];

            if (this.replyAll) {
                this.cc = this.message.cc.join(", ");
                this.to = ((this.message.headers.find(h => h.name.localeCompare("Reply-To"))?.value ?? this.message.from) + ", " + this.message.to.join(","));
            } else {
                this.cc = "";
                this.to = this.message.headers.find(h => h.name.localeCompare("Reply-To"))?.value ?? this.message.from;

            }

            this.bcc = "";


            let messageHtml: string;

            if (this.message.hasHtmlBody) {
                messageHtml = await new MessagesController().getMessageHtml(this.message.id);
            } else {
                const text = await new MessagesController().getMessagePlainText(this.message.id);

                const textArea = document.createElement("textarea");
                textArea.innerText = text;
                messageHtml = textArea.innerHTML.split("<br>").join("\n");
            }

            this.body = `<br><br>On ${this.message.receivedDate} ${this.message.from} wrote:<br><br><blockquote type="cite">${messageHtml}</blockquote>`;
        }

    }




    export default toNative(MessageReply);
</script>