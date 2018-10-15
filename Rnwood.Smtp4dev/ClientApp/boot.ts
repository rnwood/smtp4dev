import './css/site.css';
import Vue from 'vue';
import VueRouter from 'vue-router';
import Element from 'element-ui';

Vue.use(Element);
Vue.use(VueRouter);


const routes = [
    { path: '/', component: (<any> require('./components/home/home.vue.html')).default },
];

new Vue({
    el: '#app-root',
    router: new VueRouter({ mode: 'history', routes: routes }),
    render: h => h((<any> require('./components/app/app.vue.html')).default)
});

