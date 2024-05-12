<template>
    <div v-loading="loading">
        <el-button v-if="server && server.isRunning" :disabled="!server || !server.settingsAreEditable" @click="stop" icon="circle-check" link :title="'Stop'+ ((!server || !server.settingsAreEditable) ? ' - Disabled by config' : '')"> SMTP server listening on port {{server.port}}</el-button>
        <el-button v-if="server && !server.isRunning && !server.exception" :disabled="!server || !server.settingsAreEditable" @click="start" icon="circle-close" type="danger" :title="'Start'+ ((!server || !server.settingsAreEditable) ? ' - Disabled by config' : '')"> SMTP server stopped</el-button>
        <el-button v-if="server && !server.isRunning && server.exception" :disabled="!server || !server.settingsAreEditable" @click="start" icon="circle-close" type="danger" :title="'Start'+ ((!server || !server.settingsAreEditable) ? ' - Disabled by config' : '')"> SMTP server error:<br />{{server.exception}}</el-button>
        <el-button style="font-size: 1.7em; padding: 6px;" circle icon="setting" title="Settings" @click="showSettings"></el-button>&nbsp;
    </div>

</template>

<script lang="ts">

    import { Component, Vue, Prop, Watch, toNative, Emit } from "vue-facing-decorator";
    import HubConnectionManager from "../HubConnectionManager";
    import { Mutex } from "async-mutex";
    import ServerController from "../ApiClient/ServerController";
    import Server from "../ApiClient/Server";

    @Component
    class ServerStatus extends Vue {

        @Prop({ default: null })
        connection: HubConnectionManager | null = null;

        error: Error | null = null;
        private mutex = new Mutex();
        loading: boolean = true;
        server: Server | null = null;

        async refresh(silent: boolean = false) {
            var unlock = await this.mutex.acquire();

            try {
                this.error = null;
                this.loading = !silent;

                this.server = await new ServerController().getServer();
            } catch (e: any) {
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

        @Emit("showsettings")
        showSettings() {
            return;
        }
    }

    export default toNative(ServerStatus)
</script>