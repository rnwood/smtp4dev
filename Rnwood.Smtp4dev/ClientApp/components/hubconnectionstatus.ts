import Vue from "vue";
import { Component, Prop } from "vue-property-decorator";
import HubConnectionManager from "../HubConnectionManager";

@Component
export default class HubConnectionStatus extends Vue {

    constructor() {
        super();
    }

    @Prop()
    connection: HubConnectionManager | null = null;

    buttonType(): string {
        return this.connection && this.connection.connected ? "success" : this.connection && this.connection.started ? "warning" : "danger";
    }

    buttonText(): string {
        return this.connection && this.connection.connected ? "Connected" : this.connection && this.connection.started ? "Reconnecting..." : "Not connected";
    }

    buttonTitle(): string {
        var errorText = this.connection && this.connection.error ? "\nError: " + this.connection.error.message : "";

        return (this.connection && this.connection.started ? "Click to disconnect" : "Click to connect") + errorText;
    }

    buttonIcon(): string {
        return this.connection && this.connection.connected ? "el-icon-refresh" : this.connection && this.connection.started ? "el-icon-warning" : "el-icon-warning";
    }


    buttonClick = () => {
        if (this.connection) {

            if (!this.connection.started) {
                this.connection.start();
            } else {
                this.connection.stop();
            }
        }
    }

    async created() {

    }

    async destroyed() {

    }

}