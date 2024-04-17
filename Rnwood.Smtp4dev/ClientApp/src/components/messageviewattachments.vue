<template>
    <div class="vfillpanel pad" v-show="attachments">
        <div v-for="attachment in attachments" :key="attachment.id"><el-button icon="paperclip" size="small" type="primary" round v-on:click="openAttachment(attachment)">{{attachment.fileName}}</el-button></div>
    </div>
</template>
<script lang="ts">
    import { Component, Vue, Prop, Watch, toNative } from 'vue-facing-decorator'

import MessagesController from "../ApiClient/MessagesController";
import Message from "../ApiClient/Message";
import AttachmentSummary from "../ApiClient/AttachmentSummary";
import MessageEntitySummary from "../ApiClient/MessageEntitySummary";

@Component
 class MessageViewAttachments extends Vue {

    @Prop({ default: null })
    message: Message | null = null;
    attachments: AttachmentSummary[] | null = null;


    @Watch("message")
    async onMessageChanged(value: Message | null, oldValue: Message | null) {

        await this.setAttachments();
    }

    async setAttachments() {
        var result: AttachmentSummary[] = [];

        if (this.message != null) {

            var parts = this.message.parts
            this.getAttachments(parts, result);

        }
        this.attachments = result;
    }

    getAttachments(parts: MessageEntitySummary[], result: AttachmentSummary[]) {
        for (let part of parts) {
            for (let attachment of part.attachments) {
                result.push(attachment);
            }

            this.getAttachments(part.childParts, result);
        }
    }

    openAttachment(attachment: AttachmentSummary) {
        window.open(attachment.url);
    }

    async created() {

        await this.setAttachments();
    }

    async destroyed() {

    }
    }

    export default toNative(MessageViewAttachments)
</script>