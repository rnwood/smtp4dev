import { Component, Prop, Watch } from 'vue-property-decorator';
import Vue from 'vue';
import Message from "../ApiClient/Message";
import AttachmentSummary from "../ApiClient/AttachmentSummary";
import MessageEntitySummary from "../ApiClient/MessageEntitySummary";

@Component({
    template: require('./messageviewattachments.html')
})
export default class MessageViewAttachments extends Vue {
    constructor() {
        super(); 
    }

    @Prop({ default: null })
    message: Message | null = null;
    attachments: AttachmentSummary[] | null = null;


    @Watch("message")
    async onMessageChanged(value: Message, oldValue: Message) {
        var result: AttachmentSummary[] = [];
        if (value !== null) {
            this.getAttachments(value.parts, result);            
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

     
    }

    async destroyed() {
        
    }
}