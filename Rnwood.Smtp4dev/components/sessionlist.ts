import Component from "vue-class-component";
import Vue from 'vue'
import { HubConnection } from '@aspnet/signalr-client'
import SessionsController from "../ApiClient/SessionsController";
import SessionSummary from "../ApiClient/SessionSummary";
import BaseUrlProvider from '../BaseUrlProvider';

@Component({
    template: require('./sessionlist.html')
})
export default class SessionList extends Vue {


    constructor() {
        super();
    }

    private connection: HubConnection;

    sessions: SessionSummary[] = [];
    error: Error | null = null;
    selectedsession: SessionSummary | null = null;
    loading = true;

    handleCurrentChange(session: SessionSummary | null): void {
        this.selectedsession = session;
        this.$emit("selected-session-changed", session);
    }

    async clear() {

        try {
            await new SessionsController().deleteAll();
            this.refresh();
        } catch (e) {
            this.error = e;
        }

    }

    async refresh() {

        this.error = null;

        try {
            this.sessions = await new SessionsController().getSummaries();
        } catch (e) {
            this.error = e;

        } finally {
            this.loading = false;
        }

    }

    async created() {
        let baseUrl = new BaseUrlProvider().getBaseUrl();
        this.connection = new HubConnection(baseUrl + 'hubs/sessions');

        this.connection.on('sessionschanged', data => {
            this.refresh();
        });

        await this.connection.start();

        this.refresh();
    }

    async destroyed() {
        this.connection.stop();
    }
}