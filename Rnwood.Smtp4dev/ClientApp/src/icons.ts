import Vue from "vue";
import { library } from "@fortawesome/fontawesome-svg-core";
import { faEnvelopeOpen } from "@fortawesome/free-solid-svg-icons";
import { FontAwesomeIcon } from "@fortawesome/vue-fontawesome";

library.add(faEnvelopeOpen);

Vue.component("font-awesome-icon", FontAwesomeIcon);
