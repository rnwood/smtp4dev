import Vue from "vue";
import Messagelist from './components/messagelist';
import Element from 'element-ui';

Vue.use(Element);


var app = new Vue({
    el: '#app',
    components: { Messagelist }
});
