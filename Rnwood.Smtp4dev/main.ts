import 'es6-collections';
import 'element-theme-default/lib/index.css';

import Vue from "vue";
import MessageList from './components/messagelist';
import MessageView from './components/messageview';
import Component from "vue-class-component";
import MessageHeader from "ApiClient/MessageHeader";

import Element from 'element-ui';
import axios from 'axios' 

Vue.use(Element);

@Component({})
export default class Main extends Vue {

    selectedMessage: MessageHeader | null = null;

    selectedMessageChanged(selectedMessage: MessageHeader | null) {
        this.selectedMessage = selectedMessage;
    };

}

new Main({
    el: '#app',
    components: { messagelist: MessageList, messageview: MessageView }
});