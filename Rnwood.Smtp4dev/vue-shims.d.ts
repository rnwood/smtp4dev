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
    interface AceEditor {

    }

    var aceEditor: AceEditor;

    export default aceEditor;
}

declare module "locale-index-of" {

    export default function (intl: typeof Intl): (string: string, substring: string, locales: string | string[] | undefined, options: Intl.CollatorOptions | undefined) => number;

    export function prollyfill(): void;
}


interface String {
    localeIndexOf(substring: string, locales?: string | string[] | undefined, options?: Intl.CollatorOptions | undefined): number;
}
