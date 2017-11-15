import 'es6-collections';
import 'element-theme-default/lib/index.css';

import Vue from "vue";
import MessageList from './components/messagelist';
import MessageView from './components/messageview';

import Element from 'element-ui';
Vue.use(Element);

var app = new Vue({
    el: '#app',
    components: { messagelist: MessageList, messageview: MessageView }
});
