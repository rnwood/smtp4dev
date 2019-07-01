import { Component } from 'vue-property-decorator';
import Vue from 'vue'
import SessionsController from "../ApiClient/SessionsController";
import SessionSummary from "../ApiClient/SessionSummary";
import * as moment from 'moment';
import HubConnectionManager from '../HubConnectionManager';
import sortedArraySync from '../sortedArraySync';
import { Mutex } from 'async-mutex';

@Component({
    components: {
        hubconnstatus: (<any>require('./hubconnectionstatus.vue.html')).default
    }
})
export default class SessionList extends Vue {


    constructor() {
        super();

        this.connection = new HubConnectionManager('hubs/sessions', this.refresh);
        this.connection.on('sessionschanged', () => {
            this.refresh();
        });
        this.connection.start();
    }

    connection: HubConnectionManager;

    sessions: SessionSummary[] = [];
    error: Error | null = null;
    selectedsession: SessionSummary | null = null;
    loading = false;
    private mutex = new Mutex();

    handleCurrentChange(session: SessionSummary | null): void {
        this.selectedsession = session;
        this.$emit("selected-session-changed", session);
    }

    formatDate(row: number, column: number, cellValue: Date, index: number): string {
        return moment(cellValue).format('YYYY-MM-DD HH:mm:ss');
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

    refresh = async () => {

        var unlock = await this.mutex.acquire();

        try {
            this.error = null;
            this.loading = true;

            var newSessions = await new SessionsController().getSummaries();
            sortedArraySync(newSessions, this.sessions, (a: SessionSummary, b: SessionSummary) => a.id == b.id);

        } catch (e) {
            this.error = e;

        } finally {
            this.loading = false;
            unlock();
        }

    }

    async created() {

        this.refresh();
    }

    async destroyed() {
        this.connection.stop();
    }
}