import './css/site.css';
import Vue from 'vue';
import VueRouter from 'vue-router';
import Element from 'element-ui';
import axios from "axios";
import {prollyfill as localeIndexOfProllyfill } from "locale-index-of";
import Home from '@/components/home/home.vue';
import App from '@/components/app/app.vue';

const supportedBrowser = typeof (document.createElement("p").style.flex) != "undefined" && Object.prototype.hasOwnProperty.call(window, "Reflect") && Object.prototype.hasOwnProperty.call(window, "Promise");

if (!supportedBrowser) {

    (<HTMLElement>document.getElementById("loading")).style.display = "none";
    (<HTMLElement>document.getElementById("oldbrowser")).style.display = "block";

} else {

    Vue.use(Element);
    Vue.use(VueRouter);

    const routes = [
        { path: '/', component: Home },
    ];

    const router = new VueRouter({
        base: location.pathname,
        mode: 'history',
        routes: routes
    })
    const app = new Vue({
        el: '#app-root',
        router: router,
        render: h => h(App)
    });



    axios.interceptors.response.use(response => {

        fixDates(response.data);
        return response;
    });

    localeIndexOfProllyfill();
}

const dateRegex = /^(\d{4})-(\d{2})-(\d{2})T(\d{2}):(\d{2}):(\d{2}(?:\.\d*))(?:Z|(\+|-)([\d|:]*))?$/;

function fixDates(data: any) {

    if (data instanceof Array) {
        for (const item of data) {
            fixDates(item)
        }
    } else {
        for (const property in data) {
            const value = data[property];

            if (typeof value === "string") {
                if (dateRegex.test(value)) {
                    data[property] = new Date(value);
                }
            } else {
                fixDates(value);
            }
        }
    }
}




