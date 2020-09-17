<template>
    <el-dialog title="Settings" :visible.sync="visible" width="80%" :close-on-click-modal="false" :open="refresh" :before-close="handleClose">
        <div v-loading="loading">
            <el-form v-if="server">
                <el-tabs tab-position="top">
                    <el-tab-pane label="SMTP Server">
                        <el-form-item label="Hostname">
                            <el-input required v-model="server.hostName" />
                        </el-form-item>

                        <el-form-item label="Port Number">
                            <el-input-number required min=1 max=65535 controls-position="right" v-model="server.portNumber" />
                        </el-form-item>

                        <el-form-item label="Allow Remote Connections">
                            <el-switch v-model="server.allowRemoteConnections" />
                        </el-form-item>
                    </el-tab-pane>




                    <el-tab-pane label="Limits">

                        <el-form-item label="# of Messages to Keep">
                            <el-input-number required min=1 controls-position="right" v-model="server.numberOfMessagesToKeep" />
                        </el-form-item>

                        <el-form-item label="# of Sessions to Keep">
                            <el-input-number required min=1 controls-position="right" v-model="server.numberOfSessionsToKeep" />
                        </el-form-item>
                    </el-tab-pane>
                    <el-tab-pane label="Message Relay">
                        <el-form-item label="SMTP server">
                            <el-input v-model="server.relayOptions.smtpServer" />
                        </el-form-item>

                        <el-form-item label="SMTP port">
                            <el-input v-model="server.relayOptions.smtpPort" />
                        </el-form-item>

                        <el-form-item label="Login">
                            <el-input v-model="server.relayOptions.login" />
                        </el-form-item>

                        <el-form-item label="Password">
                            <el-input type="password" v-model="server.relayOptions.password" />
                        </el-form-item>

                        <el-form-item label="Sender Address">
                            <el-input v-model="server.relayOptions.senderAddress" />
                        </el-form-item>

                        <el-form-item label="Auto Relay Recipients">
                            <div v-for="(email, index) in server.relayOptions.allowedEmails" :key="index">
                                <el-input v-model="server.relayOptions.allowedEmails[index]">
                                    <el-button slot="append" @click="server.relayOptions.allowedEmails.splice(index, 1)">Remove</el-button>
                                </el-input>
                            </div>
                            <el-button size="small" @click="server.relayOptions.allowedEmails.push('')">Add</el-button>
                        </el-form-item>
                    </el-tab-pane>
                </el-tabs>
            </el-form>
        </div>

        <span slot="footer" class="dialog-footer">
            <el-button @click="save" :disabled="saving" :loading="saving">Save</el-button>
        </span>
    </el-dialog>
</template>

<script lang="ts">
    import Vue from "vue";
    import { Component, Prop, Watch } from "vue-property-decorator";
    import HubConnectionManager from "../HubConnectionManager";
    import { Mutex } from "async-mutex";
    import ServerController from "../ApiClient/ServerController";
    import Server from "../ApiClient/Server";

    @Component
    export default class SettingsDialog extends Vue {

        constructor() {
            super();
        }

        @Prop({ default: null })
        connection: HubConnectionManager | null = null;

        error: Error | null = null;
        private mutex = new Mutex();
        loading: boolean = true;
        saving: boolean = false;
        server: Server | null = null;

        @Prop({ default: false })
        visible: boolean = false;

        handleClose() {
            this.$emit('closed');
        }

        async save() {
            this.saving = true;
            try {
                await new ServerController().updateServer(this.server!);
                this.$emit('closed');
            } finally {
                this.saving = false;
            }
        }

        async refresh(silent: boolean = false) {
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