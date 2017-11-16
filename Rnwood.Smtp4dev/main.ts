import 'es6-collections';
import 'element-theme-default/lib/index.css';

import Vue from "vue";
import MessageList from './components/messagelist';
import MessageView from './components/messageview';
import Component from "vue-class-component";

import Element from 'element-ui';
Vue.use(Element);

@Component({})
export default class Main extends Vue {

    selectedMessage: Api.Message | null = null;

    selectedMessageChanged(selectedMessage: Api.Message | null) {
        this.selectedMessage = selectedMessage;
    };

}

new Main({
    el: '#app',
    components: { messagelist: MessageList, messageview: MessageView }
});