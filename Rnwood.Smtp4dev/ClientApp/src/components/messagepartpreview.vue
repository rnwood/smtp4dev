<template>
    <div v-if="isPreviewable && contentUrl" class="messagepartpreview hfillpanel">
        <iframe :src="contentUrl" frameborder="0" class="fill" />
    </div>
    <div v-else class="fill nodetails centrecontents">
        <div>Not previewable.</div>
    </div>
</template>

<script lang="ts">
    import { Component, Prop, Vue, toNative } from 'vue-facing-decorator';
    import MessageEntitySummary from '../ApiClient/MessageEntitySummary';
    import Message from '../ApiClient/Message';
    import MessagesController from '../ApiClient/MessagesController';

    @Component({})
    class MessagePartPreview extends Vue {
    @Prop({ required: true })
    message!: Message;

    @Prop({ required: true })
    part!: MessageEntitySummary;

    get contentType(): string {
    const header = this.part.headers.find(h => h.name.toLowerCase() === 'content-type');
    return header ? header.value.split(';')[0].trim().toLowerCase() : '';
    }

    get isPreviewable(): boolean {
    // Add more types as needed (e.g., text/html, text/plain, application/pdf)
    return this.contentType.startsWith('image/') || this.contentType === 'application/pdf';
    }

    get contentUrl(): string | null {
    if (!this.isPreviewable) return null;
    return new MessagesController().getPartContent_url(this.message.id, this.part.id, false);
    }
    }

    export default toNative(MessagePartPreview);
</script>

<style scoped>
    .messagepartpreview {
    margin-top: 1em;
    }
    .messagepartpreview__not-previewable {
    color: #888;
    font-size: 0.95em;
    margin-top: 1em;
    }
</style>
