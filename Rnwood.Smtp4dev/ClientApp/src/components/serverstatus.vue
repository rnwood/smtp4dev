<template>
    <div v-loading="loading">
        <el-button v-if="server && server.isRunning" @click="stop" icon="el-icon-circle-check" type="text"> SMTP server listening on port {{server.portNumber}}</el-button>
        <el-button v-if="server && !server.isRunning && !server.exception" @click="start" icon="el-icon-circle-close" type="danger"> SMTP server stopped</el-button>
        <el-button v-if="server && !server.isRunning && server.exception" @click="start" icon="el-icon-circle-close" type="danger"> SMTP server error:<br/>{{server.exception}}</el-button>
    </div>
</template>

<script lang="ts">
    import Vue from "vue";
    import { Component, Prop, Watch } from "vue-property-decorator";
    import HubConnectionManager from "../HubConnectionManager";
    import { Mutex } from "async-mutex";
    import ServerController from "../ApiClient/ServerController";
    import Server from "../ApiClient/Server";

    @Component
    export default class ServerStatus extends Vue {

        constructor() {
            super();
        }

        @Prop({ default: null })
        connection: HubConnectionManager | null = null;

        error: Error | null = null;
        private mutex = new Mutex();
        loading: boolean = true;
        server: Server | null = null;

        async refresh(silent: boolean=false) {
            var unlock = await this.mutex.acquire();

            try {
                this.error = null;
                this.loading = !silent;

                this.server = await new ServerController().getServer();
            } catch (e) {
                this.error = e;
            } finally {
                this.loading = false;
                unlock();
            }
        }

        async stop() {
            let serverUpdate: Server = { ... this.server } as Server;
            serverUpdate.isRunning = false;

            await new ServerController().updateServer(serverUpdate);
        }

        async start() {
            let serverUpdate: Server = { ... this.server } as Server;
            serverUpdate.isRunning = true;
            await new ServerController().updateServer(serverUpdate);
        }

        async mounted() {
            await this.refresh();
        }

        @Watch("connection")
        onConnectionChanged() {
            if (this.connection) {
                this.connection.on("serverchanged", async () => {
                    await this.refresh();
                });

                this.connection.addOnConnectedCallback(() => this.refresh());
            }
        }
    }
</script>