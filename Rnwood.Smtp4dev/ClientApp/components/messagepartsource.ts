import { Component, Prop,Watch } from 'vue-property-decorator'
import Vue from 'vue'
import MessagesController from "../ApiClient/MessagesController";
import MessageEntitySummary from "../ApiClient/MessageEntitySummary";

@Component
export default class MessagePartSource extends Vue {
    constructor() {
        super(); 
    }

    @Prop()
    messageEntitySummary: MessageEntitySummary | null = null;

    source: string | null = null;
    sourceurl: string | null = null;
    error: Error | null = null;
    loading = false;

    @Prop()
    type: string = "source";

    @Watch("messageEntitySummary")
    async onMessageEntitySummaryChanged(value: MessageEntitySummary, oldValue: MessageEntitySummary) {
        
        await this.loadMessage();
        
    }

    download() {
        if (this.sourceurl) {
            window.open(this.sourceurl);
        }
    }

    async loadMessage() {
        
        this.error = null;
        this.loading = true;
        this.source = null;
        this.sourceurl = null;

        try {
            if (this.messageEntitySummary != null) {
                if (this.type == "raw") {
                    this.sourceurl = new MessagesController().getPartSourceRaw_url(this.messageEntitySummary.messageId, this.messageEntitySummary.contentId);

                    if (this.messageEntitySummary.size > 5 * 1024 * 1024) {
                        this.source = "Large content cannot be shown here. Click above to download";
                    } else {
                        this.source = await new MessagesController().getPartSourceRaw(this.messageEntitySummary.messageId, this.messageEntitySummary.contentId);
                    }
                } else {
                    this.sourceurl = new MessagesController().getPartSource_url(this.messageEntitySummary.messageId, this.messageEntitySummary.contentId);
                    if (this.messageEntitySummary.size > 5 * 1024 * 1024) {
                        this.source = "Large content cannot be shown here. Click above to download";
                    } else {
                        this.source = await new MessagesController().getPartSource(this.messageEntitySummary.messageId, this.messageEntitySummary.contentId);
                    }
                    
                }
            }
        } catch (e) {
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