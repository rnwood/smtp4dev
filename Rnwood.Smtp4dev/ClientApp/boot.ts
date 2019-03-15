import './css/site.css';
import Vue from 'vue';
import VueRouter from 'vue-router';
import Element from 'element-ui';

var supportedBrowser = typeof (document.createElement("p").style.flex) != "undefined" && window.hasOwnProperty("Reflect") && window.hasOwnProperty("Promise");

if (!supportedBrowser) {

    (<HTMLElement>document.getElementById("oldbrowser")).style.display = "block";
    (<HTMLElement>document.getElementById("app-root")).style.display = "none";

} else {

    Vue.use(Element);
    Vue.use(VueRouter);

    const routes = [
        { path: '/', component: (<any>require('./components/home/home.vue.html')).default },
    ];

    var router = new VueRouter({ mode: 'history', routes: routes })
    var app = new Vue({
        el: '#app-root',
        router: router,
        render: h => h((<any>require('./components/app/app.vue.html')).default)
    });

}




