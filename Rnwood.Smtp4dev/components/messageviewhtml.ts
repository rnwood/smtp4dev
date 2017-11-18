import { Component, Prop,Watch } from 'vue-property-decorator'
import Vue from 'vue'
import MessagesController from "../ApiClient/MessagesController";
import MessageSummary from "../ApiClient/MessageSummary";

@Component({
    template: require('./messageviewhtml.html')
})
export default class MessageViewHtml extends Vue {
    constructor() {
        super(); 
    }

    @Prop({ default: null })
    messageSummary: MessageSummary | null = null;
    html: string | null = null;


    error: Error | null = null;
    loading = false;

    @Watch("messageSummary")
    async onMessageChanged(value: MessageSummary, oldValue: MessageSummary) {
        
        await this.loadMessage();
        
    }

    async loadMessage() {
        
        this.error = null;
        this.loading = true;
        this.html = null;

        try {
            if (this.messageSummary != null) {
                this.html = await new MessagesController().getMessageHtml(this.messageSummary.id);
            }
        } catch (e) {
            this.error = e;
        } finally {
            this.loading = false;
        }   
    }

    async created() {

     
    }

    async destroyed() {
        
    }
}