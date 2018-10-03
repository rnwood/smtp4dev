declare module "*.vue" {
    import Vue from "vue";
    export default Vue;
}

declare var require: any

declare module "srcdoc-polyfill" {
   
    export function set(element: any, html : string): void;
}