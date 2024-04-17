import {App } from 'vue'
import { library } from "@fortawesome/fontawesome-svg-core";
import { faEnvelopeOpen } from "@fortawesome/free-solid-svg-icons";
import * as FontAwesomeIcons from "@fortawesome/vue-fontawesome";
import * as ElementPlusIconsVue from '@element-plus/icons-vue'

export default function useIcons(app: App) {
    library.add(faEnvelopeOpen);

    for (const [key, component] of Object.entries(FontAwesomeIcons)) {
        app.component(key, component)
    }

    for (const [key, component] of Object.entries(ElementPlusIconsVue)) {
        app.component(key, component)
    }
}

