import { Component, Prop,Watch } from 'vue-property-decorator'
import Vue from 'vue'
import MessagesController from "../ApiClient/MessagesController";
import MessageSummary from "../ApiClient/MessageSummary";
import Message from "../ApiClient/Message";
import MessageEntitySummary from "../ApiClient/MessageEntitySummary";

@Component({ 
    components: {
        headers: (<any>require('./headers.vue.html')).default,
        "messageview-html": (<any>require('./messageviewhtml.vue.html')).default,
        "messageviewattachments": (<any>require('./messageviewattachments.vue.html')).default,
        "messagepartsource": (<any>require('./messagepartsource.vue.html')).default
    }
})
export default class MessageView extends Vue {
    constructor() {
        super(); 
    }

    @Prop({})
    messageSummary: MessageSummary | null = null;
    message: Message | null = null;
    selectedPart: MessageEntitySummary | null = null;


    error: Error | null = null;
    loading = false;

    @Watch("messageSummary")
    async onMessageSummaryChange(value: MessageSummary|null, oldValue: MessageSummary|null) {
        
        await this.loadMessage();
        
    }


    async loadMessage() {
        
        this.error = null;
        this.loading = true;
        this.message = null;
        this.selectedPart = null;

        try {
            if (this.messageSummary != null) {

                this.message = await new MessagesController().getMessage(this.messageSummary.id);

                if (this.messageSummary.isUnread) {
                    var currentMessageSummary = this.messageSummary;
                    setTimeout(async () => {
                        if (this.messageSummary != null && currentMessageSummary.id == this.messageSummary.id) {
                            try {
                                await new MessagesController().markMessageRead(this.messageSummary.id);
                            } catch (e) {
                                console.error(e);
                            }
                        }
                    }, 2000)
                }
                
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

    onPartSelection(part: MessageEntitySummary|null) {
        this.selectedPart = part;
    }

    async download() {
        if (this.messageSummary == null) {
            return;
        }
        window.open(new MessagesController().downloadMessage_url(this.messageSummary.id))
    }


    async mounted() {
        await this.loadMessage();
     
    }

    async destroyed() {
        
    }

    get headers() {
        return this.message != null ? this.message.headers : [];
    }

    get selectedPartHeaders() {
        return this.selectedPart != null ? this.selectedPart.headers : [];
    }
}