import { Component, Prop, Watch } from 'vue-property-decorator'
import Vue from 'vue'
import SessionsController from "../ApiClient/SessionsController";
import SessionSummary from "../ApiClient/SessionSummary";
import Session from "../ApiClient/Session";

@Component({
    components: {
        textview: (<any>require('./textview.vue.html')).default
    }
})
export default class SessionView extends Vue {
    constructor() {
        super();
    }

    @Prop({})
    sessionSummary: SessionSummary | null = null;
    session: Session | null = null;
    log: string | null = null;


    error: Error | null = null;
    loading = false;

    @Watch("sessionSummary")
    async onMessageChanged(value: SessionSummary | null, oldValue: SessionSummary | null) {

        await this.loadSession();

    }

    download() {
        if (this.sessionSummary) {
            window.open(new SessionsController().getSessionLog_url(this.sessionSummary.id));
        }
    }

    async loadSession() {

        this.error = null;
        this.loading = true;
        this.session = null;
        this.log = null;

        try {
            if (this.sessionSummary != null) {
                this.session = await new SessionsController().getSession(this.sessionSummary.id);
                this.log = await new SessionsController().getSessionLog(this.sessionSummary.id);
            }
        } catch (e) {
            this.error = e;
        } finally {
            this.loading = false;
        }
    }



    async created() {


    }

    async destroyed() {

    }
}