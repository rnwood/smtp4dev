import './css/site.css';
import { createApp } from 'vue';
import { createRouter, createWebHashHistory } from 'vue-router';
import Element from 'element-plus';
import axios from "axios";
import Home from '@/components/home/home.vue';
import App from '@/components/app/app.vue';
import useIcons from './icons';
import ClientSettingsManager from "@/ApiClient/ClientSettingsManager";

// Manage initial theme based on client settings (stored value first for no-flash),
// then merge with server defaults when available. Also listen for changes to
// the client settings so the UI updates immediately when the user changes the preference.
// Keep track of current client dark mode mode ("dark" | "light" | "follow")
let currentClientDarkMode: string | null = null;

const prefersDarkMedia = window.matchMedia ? window.matchMedia('(prefers-color-scheme: dark)') : null;
// Listener to update the page when system preference changes while in "follow" mode
let systemPrefListener: ((e: MediaQueryListEvent) => void) | null = null;

function applyDarkMode(mode: string | null) {
    currentClientDarkMode = mode ?? 'follow';

    if (mode === 'dark') {
        document.documentElement.classList.add('dark');
        // stop listening to system changes
        if (prefersDarkMedia && systemPrefListener) prefersDarkMedia.removeEventListener('change', systemPrefListener);
    } else if (mode === 'light') {
        document.documentElement.classList.remove('dark');
        if (prefersDarkMedia && systemPrefListener) prefersDarkMedia.removeEventListener('change', systemPrefListener);
    } else {
        // follow system
        const systemDark = prefersDarkMedia ? prefersDarkMedia.matches : false;
        if (systemDark) document.documentElement.classList.add('dark');
        else document.documentElement.classList.remove('dark');

        // ensure we are listening for changes
        if (prefersDarkMedia) {
            // remove previous listener first
            if (systemPrefListener) prefersDarkMedia.removeEventListener('change', systemPrefListener);
            systemPrefListener = (e: MediaQueryListEvent) => {
                if (currentClientDarkMode === 'follow') {
                    if (e.matches) document.documentElement.classList.add('dark');
                    else document.documentElement.classList.remove('dark');
                }
            };
            prefersDarkMedia.addEventListener('change', systemPrefListener);
        }
    }
}

// Apply any locally stored setting immediately to avoid flicker on load
try {
    const stored = ClientSettingsManager.getStoredSettings();
    if (stored && stored.darkMode) {
        applyDarkMode(stored.darkMode);
    }
} catch (e) {
    console.warn('Failed to read stored client settings for dark mode:', e);
}

// When the merged server+stored settings become available, apply them
ClientSettingsManager.getClientSettings().then(s => {
    if (s && s.darkMode) applyDarkMode(s.darkMode);
}).catch(e => console.warn('Failed to load client settings for dark mode:', e));

// Listen for live changes to client settings (e.g., via settings dialog save)
ClientSettingsManager.onSettingsChanged((oldSettings, newSettings) => {
    try {
        applyDarkMode(newSettings.darkMode);
    } catch (e) {
        console.error('Error applying dark mode from client settings change:', e);
    }
});

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
        { path: '/messages', component: Home },
        { path: '/messages/mailbox/:mailbox', component: Home },
        { path: '/sessions', component: Home },
        { path: '/sessions/session/:sessionId', component: Home },
        { path: '/serverlog', component: Home },
    ];

    const router = createRouter({
        history: createWebHashHistory(),
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




