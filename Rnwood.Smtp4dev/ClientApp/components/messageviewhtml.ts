import { Component, Prop,Watch } from 'vue-property-decorator'
import Vue from 'vue'
import MessagesController from "../ApiClient/MessagesController";
import Message from "../ApiClient/Message";
import * as srcDoc from 'srcdoc-polyfill';

@Component
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
        srcDoc.set(<HTMLIFrameElement>this.$refs.htmlframe, value);
    }

    async onHtmlFrameLoaded() {
        var doc = (<HTMLIFrameElement> this.$refs.htmlframe).contentDocument;
        if (!doc) {
            return;
        }

        var baseElement = doc.body.querySelector("base") || doc.createElement("base");
        baseElement.setAttribute("target", "_blank");

        doc.body.appendChild(baseElement);
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

        this.loadMessage();
    }

    async destroyed() {
        
    }
}