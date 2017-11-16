import { Component, Prop,Watch } from 'vue-property-decorator'
import Vue from 'vue'
import MessagesController from "../ApiClient/MessagesController";
import MessageHeader from "../ApiClient/MessageHeader";
import Message from "../ApiClient/Message";

@Component({
    template: require('./messageview.html')
})
export default class MessageView extends Vue {
    constructor() {
        super(); 
    }

    @Prop({ default: null })
    messageHeader: MessageHeader | null = null;

    message: Message | null = null;


    error: Error | null = null;
    loading = false;

    @Watch("messageHeader")
    async onMessageChanged(value: MessageHeader, oldValue: MessageHeader) {
        this.message = null;

        if (value != null) {
            this.loading = true;

            try {
                this.message = await new MessagesController().getMessage(value.id);
            } catch (e) {
                this.error = e;
            } finally {
                this.loading = false;
            }
        }
    }


    async created() {

     
    }

    async destroyed() {
        
    }
}