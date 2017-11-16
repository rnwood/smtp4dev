import Component from "vue-class-component";
import Vue from 'vue'
import axios from 'axios';
import Message = Api.Message;

@Component({
    template: require('./messageview.html'),
    props: ["message"]
})
export default class MessageView extends Vue {
    constructor() {
        super();
    }
   
    message: Message | null;
    error: Error | null = null;

    async created() {

     
    }

    async destroyed() {
        
    }
}