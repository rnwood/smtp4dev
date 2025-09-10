declare module 'brace' {
    export default any;
    export function acequire(module: string): any;
}

declare global {
    interface Window {
        ace: any;
    }
}

export {};