import Component from "vue-class-component";
import Vue from 'vue'
import axios from 'axios';
import Message = Api.Message;

@Component({
    template: require('./messageview.html')
})
export default class MessageView extends Vue {
    constructor() {
        super();
    }
   
    message?: Message;
    error?: Error;

    async created() {

     
    }

    async destroyed() {
        
    }
}