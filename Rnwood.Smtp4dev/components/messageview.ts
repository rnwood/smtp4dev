import { Component, Prop,Watch } from 'vue-property-decorator'
import Vue from 'vue'
import MessagesController from "../ApiClient/MessagesController";
import MessageSummary from "../ApiClient/MessageSummary";
import Message from "../ApiClient/Message";
import MessageEntitySummary from "../ApiClient/MessageEntitySummary";
import Headers from './headers';
import MessageViewHtml from "./messageviewhtml";
import MessageViewAttachments from "./messageviewattachments";
import MessagePartSource from "./messagepartsource";

@Component({ 
    template: require('./messageview.html'),
    components: {
        headers: Headers,
        "messageview-html": MessageViewHtml,
        "messageviewattachments" : MessageViewAttachments,
        "messagepartsource" : MessagePartSource
    }
})
export default class MessageView extends Vue {
    constructor() {
        super(); 
    }

    @Prop({ default: null })
    messageSummary: MessageSummary | null = null;
    message: Message | null = null;
    selectedPart: MessageEntitySummary | null = null;


    error: Error | null = null;
    loading = false;

    @Watch("messageSummary")
    async onMessageChanged(value: MessageSummary, oldValue: MessageSummary) {
        
        await this.loadMessage();
        
    }

    async loadMessage() {
        
        this.error = null;
        this.loading = true;
        this.message = null;

        try {
            if (this.messageSummary != null) {
                this.message = await new MessagesController().getMessage(this.messageSummary.id);
            }
        } catch (e) {
            this.error = e;
        } finally {
            this.loading = false;
        }   
    }

    isLeaf(values: MessageEntitySummary[]) {
        return values.length == 0;
    }

    handleNodeClick(value: MessageEntitySummary) {
        this.selectedPart = value;
    }

    async download() {
        if (this.messageSummary == null) {
            return;
        }
        window.open(new MessagesController().downloadMessage_url(this.messageSummary.id))
    }


    async created() {

     
    }

    async destroyed() {
        
    }
}