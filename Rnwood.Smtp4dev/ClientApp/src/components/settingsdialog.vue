<template>
    <el-dialog title="Settings" :visible.sync="visible" width="80%" :close-on-click-modal="false" @open="refresh" :before-close="handleClose">
        <div v-loading="loading">
            <el-alert v-if="error" type="error" title="Error" show-icon>
                {{error.message}}
            </el-alert>

            <el-form v-if="server" :model="this" ref="form" :rules="rules" :disabled="saving">
                <el-tabs tab-position="top">
                    <el-tab-pane label="SMTP Server">
                        <el-form-item label="Hostname" prop="server.hostName">
                            <el-input v-model="server.hostName" />
                        </el-form-item>

                        <el-form-item label="Port Number" prop="server.portNumber">
                            <el-input-number :min=1 :max=65535 controls-position="right" v-model="server.portNumber" />
                        </el-form-item>

                        <el-form-item label="Allow Remote Connections" prop="server.allowRemoteConnections">
                            <el-switch v-model="server.allowRemoteConnections" />
                        </el-form-item>
                    </el-tab-pane>


                    <el-tab-pane label="IMAP Server">
                        <el-form-item label="Port Number" prop="server.imapPortNumber">
                            <el-input-number :min=1 :max=65535 controls-position="right" v-model="server.imapPortNumber" />
                        </el-form-item>
                    </el-tab-pane>

                    <el-tab-pane label="Limits">

                        <el-form-item label="# of Messages to Keep" prop="server.numberOfMessagesToKeep">
                            <el-input-number :min=1 controls-position="right" v-model="server.numberOfMessagesToKeep" />
                        </el-form-item>

                        <el-form-item label="# of Sessions to Keep" prop="server.numberOfSessionsToKeep">
                            <el-input-number :min=1 controls-position="right" v-model="server.numberOfSessionsToKeep" />
                        </el-form-item>
                    </el-tab-pane>
                    <el-tab-pane label="Message Relay">
                        <el-form-item label="Message Relay Enabled" prop="isRelayEnabled">
                            <el-switch v-model="isRelayEnabled" />
                        </el-form-item>

                        <el-form-item label="SMTP server" prop="server.relayOptions.smtpServer" v-show="isRelayEnabled">
                            <el-input v-model="server.relayOptions.smtpServer" />
                        </el-form-item>

                        <el-form-item label="SMTP port" prop="server.relayOptions.smtpPort" v-show="isRelayEnabled">
                            <el-input-number :min=1 :max=65535 controls-position="right" v-model="server.relayOptions.smtpPort" />
                        </el-form-item>

                        <el-form-item label="Login" prop="server.relayOptions.login" v-show="isRelayEnabled">
                            <el-input v-model="server.relayOptions.login" />
                        </el-form-item>

                        <el-form-item label="Password" prop="server.relayOptions.password" v-show="isRelayEnabled">
                            <el-input type="password" v-model="server.relayOptions.password" />
                        </el-form-item>

                        <el-form-item label="Sender Address - leave blank to use original sender" prop="server.relayOptions.senderAddress" v-show="isRelayEnabled">
                            <el-input v-model="server.relayOptions.senderAddress" />
                        </el-form-item>

                        <el-form-item label="Auto-Relay Recipients" v-show="isRelayEnabled" prop="isRelayEnabled">
                            <div v-for="(email, index) in server.relayOptions.automaticEmails" :key="index">
                                <el-form-item :prop="'relayOptionsAutomaticEmails[' + index + '].value'" :rules="{required: true, message: 'Required'}">
                                    <el-input v-model="server.relayOptions.automaticEmails[index]">
                                        <el-button slot="append" @click="server.relayOptions.automaticEmails.splice(index, 1)">Remove</el-button>
                                    </el-input>
                                </el-form-item>
                            </div>
                            <el-button size="small" @click="server.relayOptions.automaticEmails.push('')">New Auto-Relay Recipient</el-button>
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
    import { Form } from 'element-ui'
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

        get rules() {
            let result = {
                server: {
                    hostName: [{ required: true, message: 'Hostname is required' }],
                    portNumber: [{ required: true, message: 'Port number is required' }],
                    numberOfMessagesToKeep: [{ required: true, message: '# of Messages to Keep is required' }],
                    numberOfSessionsToKeep: [{ required: true, message: '# of Sessions to Keep is required' }],
                    relayOptions: {
                        smtpServer: <object[]>[],
                        smtpPort: <object[]>[]
                    }
                }
            };

            if (this.isRelayEnabled) {
                result.server.relayOptions.smtpServer.push({ required: true, message: "SMTP server is required if relaying enabled" });
                result.server.relayOptions.smtpPort.push({ required: true, message: "SMTP port is required if relaying enabled" });
            }

            return result;

        }

        @Prop({ default: false })
        visible: boolean = false;

        handleClose() {
           this.$emit('closed');
        }


        private isRelayEnabledValue: boolean = false;
        get isRelayEnabled() {
            return this.isRelayEnabledValue;
        }

        set isRelayEnabled(value: boolean) {
            this.isRelayEnabledValue = value;
            if (!this.isRelayEnabledValue && this.server) {
                this.server.relayOptions.smtpServer = "";
                this.server.relayOptions.automaticEmails.splice(0, this.server.relayOptions.automaticEmails.length);
            }
        }
             
        relayOptionsAutomaticEmails: any[] = [];

        @Watch("server.relayOptions.automaticEmails")
        updateAutomaticEmailsForValidation() {

            if (!this.server) {
                return;
            }

            this.relayOptionsAutomaticEmails = this.server.relayOptions.automaticEmails.map((v, i) => {
                return { value: v };
            });

        }

        async save() {
            this.saving = true;
            this.error = null;
            try {
                let valid =  await (<Form>this.$refs["form"]).validate()

                if (valid) {

                    await new ServerController().updateServer(this.server!);
                    this.handleClose();
                }
            } catch (e) {
                this.error = e;
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
                this.isRelayEnabled = !!this.server.relayOptions.smtpServer;
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