import { Component, Prop, Watch } from 'vue-property-decorator';
import Vue from 'vue';
import SessionsController from "../ApiClient/SessionsController";
import SessionSummary from "../ApiClient/SessionSummary";
import Session from "../ApiClient/Session";

@Component({ 
    template: require('./sessionview.html')
})
export default class SessionView extends Vue {
    constructor() {
        super(); 
    }

    @Prop({ default: null })
    sessionSummary: SessionSummary | null = null;
    session: Session | null = null;


    error: Error | null = null;
    loading = false;

    @Watch("sessionSummary")
    async onMessageChanged(value: SessionSummary, oldValue: SessionSummary) {
        
        await this.loadSession();
        
    }

    async loadSession() {
        
        this.error = null;
        this.loading = true;
        this.session = null;

        try {
            if (this.sessionSummary != null) {
                this.session = await new SessionsController().getSession(this.sessionSummary.id);
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