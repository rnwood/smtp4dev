import { Component, Prop, Watch } from 'vue-property-decorator';
import Vue from 'vue';
import MessagesController from "../ApiClient/MessagesController";
import MessageEntitySummary from "../ApiClient/MessageEntitySummary";

@Component({
    template: require('./messagepartsource.html')
})
export default class MessagePartSource extends Vue {
    constructor() {
        super(); 
    }

    @Prop({ default: null })
    messageEntitySummary: MessageEntitySummary | null = null;
    source: string | null = null;


    error: Error | null = null;
    loading = false;

    @Watch("messageEntitySummary")
    async onMessageEntitySummaryChanged(value: MessageEntitySummary, oldValue: MessageEntitySummary) {
        
        await this.loadMessage();
        
    }

    async loadMessage() {
        
        this.error = null;
        this.loading = true;
        this.source = null;

        try {
            if (this.messageEntitySummary != null) {
                this.source = await new MessagesController().getPartSource(this.messageEntitySummary.messageId, this.messageEntitySummary.contentId);
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