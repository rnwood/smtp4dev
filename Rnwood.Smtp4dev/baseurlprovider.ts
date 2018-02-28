﻿import 'es6-collections';
import 'element-theme-default/lib/index.css';

import Vue from "vue";
import Component from "vue-class-component";

import Element from 'element-ui';

Vue.use(Element);

@Component({})
export default class BaseUrlProvider extends Vue {
    public getBaseUrl(): string {
        let baseUrl = window.location.protocol + "//" + window.location.host;
        const pathname = window.location.pathname.replace('index.html', '');
        return baseUrl + "/" + pathname;
    }
}