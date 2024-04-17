import './css/site.css';
import { createApp } from 'vue';
import { createRouter, createWebHistory } from 'vue-router';
import Element from 'element-plus';
import axios from "axios";
import Home from '@/components/home/home.vue';
import App from '@/components/app/app.vue';
import useIcons from './icons';

const supportedBrowser = typeof (document.createElement("p").style.flex) != "undefined" && Object.prototype.hasOwnProperty.call(window, "Reflect") && Object.prototype.hasOwnProperty.call(window, "Promise");

if (!supportedBrowser) {

    (<HTMLElement>document.getElementById("loading")).style.display = "none";
    (<HTMLElement>document.getElementById("oldbrowser")).style.display = "block";

} else {

    const app = createApp(App);
    app.use(Element);
    useIcons(app);

    const routes = [
        { path: '/', component: Home },
    ];

    const router = createRouter({
        history: createWebHistory(location.pathname),
        routes: routes,
    });
    
    app.use(router);
    app.mount('#app-root')

    axios.interceptors.response.use(response => {

        fixDates(response.data);
        return response;
    });
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




