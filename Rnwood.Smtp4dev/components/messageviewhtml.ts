﻿import { Component, Prop,Watch } from 'vue-property-decorator'
import Vue from 'vue'
import MessagesController from "../ApiClient/MessagesController";
import Message from "../ApiClient/Message";

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
    async onMessageChanged(value: Message, oldValue: Message) {
        
        await this.loadMessage();
        
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

    async addAnchorTarget() {
        const iframe = document.getElementsByTagName("iframe").length > 0 ? document.getElementsByTagName("iframe")[0] : null;
        if (iframe != null && iframe.contentDocument != null) {
            const anchors = iframe.contentDocument.querySelectorAll("a");
            if (anchors != null) {
                for (let i = 0; i < anchors.length; i++) {
                    anchors[i].setAttribute("target", "_blank");
                }
            }
        }
    }
}