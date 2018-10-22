import './css/site.css';
import '../node_modules/highlight.js/styles/vs2015.css'
import Vue from 'vue';
import VueRouter from 'vue-router';
import Element from 'element-ui';
import VueHighlightJS from 'vue-highlightjs';

Vue.use(Element);
Vue.use(VueRouter);
Vue.use(VueHighlightJS)

const routes = [
    { path: '/', component: (<any> require('./components/home/home.vue.html')).default },
];

var router = new VueRouter({ mode: 'history', routes: routes })
var app = new Vue({
    el: '#app-root',
    router: router,
    render: h => h((<any>require('./components/app/app.vue.html')).default)
});







