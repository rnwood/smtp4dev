/*declare module "*.vue" {
    import Vue from "vue";
    export default Vue;
}

declare var require: any
*/

declare module "srcdoc-polyfill" {
   
    export function set(iframe: HTMLIFrameElement, html : string): void;
}

declare module "vue2-ace-editor" {
    var AceEditor: any;

    export default AceEditor;
}