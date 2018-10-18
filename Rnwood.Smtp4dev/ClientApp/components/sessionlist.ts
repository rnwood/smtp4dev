import { Component } from 'vue-property-decorator';
import Vue from 'vue'
import SessionsController from "../ApiClient/SessionsController";
import SessionSummary from "../ApiClient/SessionSummary";
import * as moment from 'moment';
import HubConnectionManager from '../HubConnectionManager';

@Component({
    components: {
        hubconnstatus: (<any>require('./hubconnectionstatus.vue.html')).default
    }
})
export default class SessionList extends Vue {
    

    constructor() {
        super();

        this.connection = new HubConnectionManager('/hubs/sessions');
        this.connection.on('sessionschanged', () => {
            this.refresh();
        });
        this.connection.start();
    }

    connection: HubConnectionManager;

    sessions: SessionSummary[] = [];
    error: Error | null = null;
    selectedsession: SessionSummary | null = null;
    loading = true;

    handleCurrentChange(session: SessionSummary | null): void {
        this.selectedsession = session;
        this.$emit("selected-session-changed", session);
    }


    formatDate(row: number, column: number, cellValue: string, index: number): string {
        return moment(String(cellValue)).format('YYYY-DD-MM hh:mm:ss');
    }

    async deleteSelected() {

        if (this.selectedsession == null) {
            return;
        }

        try {
            await new SessionsController().delete(this.selectedsession.id);
            this.refresh();
        } catch (e) {
            this.error = e;
        }

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
        this.loading = true;

        try {
            this.sessions = await new SessionsController().getSummaries();
        } catch (e) {
            this.error = e;

        } finally {
            this.loading = false;
        }

    }

    async created() {

        this.refresh();
    }

    async destroyed() {
        this.connection.stop();
    }
}