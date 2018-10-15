import Vue from 'vue';
import { Component } from 'vue-property-decorator';
import MessageSummary from "../../ApiClient/MessageSummary";
import SessionSummary from "../../ApiClient/SessionSummary";


@Component({
    components: {
        messagelist: (<any>require('../messagelist.vue.html')).default,
        messageview: (<any>require('../messageview.vue.html')).default,
        sessionlist: (<any>require('../sessionlist.vue.html')).default,
        sessionview: (<any>require('../sessionview.vue.html')).default
    }
})
export default class Home extends Vue {

    selectedMessage: MessageSummary | null = null;
    selectedSession: SessionSummary | null = null;

    selectedMessageChanged(selectedMessage: MessageSummary | null) {
        this.selectedMessage = selectedMessage;
    };

    selectedSessionChanged(selectedSession: SessionSummary | null) {
        this.selectedSession = selectedSession;
    };

}
