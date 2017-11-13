import 'es6-collections';
import Vue from "vue";
import Messagelist from './components/messagelist';
import Element from 'element-ui';
import 'element-theme-default/lib/index.css'

Vue.use(Element);


var app = new Vue({
    el: '#app',
    components: { Messagelist }
});
