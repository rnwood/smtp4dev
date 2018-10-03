import { Component, Prop,Watch } from 'vue-property-decorator'
import Vue from 'vue'
import MessagesController from "../ApiClient/MessagesController";
import Message from "../ApiClient/Message";
import * as srcDoc from 'srcdoc-polyfill';

@Component({
    template: require('./messageviewhtml.html')
})
export default class MessageViewHtml extends Vue {
    constructor() {
        super(); 
    }

    @Prop({ default: null })
    message: Message | null = null;
    html: string | null = null;


    error: Error | null = null;
    loading = false;

    @Watch("message")
    async onMessageChanged(value: Message|null, oldValue: Message|null) {
        
        await this.loadMessage();
        
    }

    @Watch("html")
    async onHtmlChanged(value: string) {
        srcDoc.set(this.$refs.htmlframe, value);
    }

    async loadMessage() {
        
        this.error = null;
        this.loading = true;
        this.html = null;
        
        try {
            if (this.message != null) {

                this.html = await new MessagesController().getMessageHtml(this.message.id);
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