import { createRouter, createWebHashHistory } from 'vue-router';
import Home from '@/components/home/home.vue';

export const routes = [
    { path: '/', component: Home },
    { path: '/messages', component: Home },
    { path: '/messages/mailbox/:mailbox/folder/:folder', component: Home },
    { path: '/messages/mailbox/:mailbox/folder/:folder/message/:message', component: Home },
    { path: '/sessions', component: Home },
    { path: '/sessions/session/:sessionId', component: Home },
    { path: '/serverlog', component: Home },
];

export const router = createRouter({
    history: createWebHashHistory(),
    routes: routes,
});

// Global router instance for class components
let globalRouter: any = null;

export function setGlobalRouter(r: any) {
    globalRouter = r;
}

export function getGlobalRouter() {
    return globalRouter;
}
