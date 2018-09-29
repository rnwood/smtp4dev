import Component from "vue-class-component";
import Vue from "vue"
import { HubConnectionBuilder, HubConnection } from "@aspnet/signalr"
import SessionsController from "../ApiClient/SessionsController";
import SessionSummary from "../ApiClient/SessionSummary";

@Component({
    template: require("./sessionlist.html")
})
export default class SessionList extends Vue {


    constructor() {
        super();

        this.connection = new HubConnectionBuilder().withUrl("/hubs/sessions").build();
        this.connection.on("sessionschanged",
            data => {
                this.refresh();
            });
    }

    private connection: HubConnection;
    private connectionStarted = false;

    sessions: SessionSummary[] = [];
    error: Error | null = null;
    selectedsession: SessionSummary | null = null;
    loading = true;

    handleCurrentChange(session: SessionSummary | null): void {
        this.selectedsession = session;
        this.$emit("selected-session-changed", session);
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

        try {

            if (!this.connectionStarted) {
                await this.connection
                    .start()
                    .then(() => console.log("Session connection started."))
                    .catch(err => console.log(`Error establishing connection (${err})`));;
                this.connectionStarted = true;
            }

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
        this.connectionStarted = false;
    }
}