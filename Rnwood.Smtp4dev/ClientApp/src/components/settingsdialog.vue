<template>
    <el-dialog title="Settings" :visible="visible" width="80%" :close-on-click-modal="false" @open="refresh" :before-close="handleClose">
        <div v-loading="loading">
            <el-alert v-if="error" type="error" title="Error" show-icon>
                {{error.message}}
            </el-alert>

            <el-form v-if="server" :model="this" ref="form" :rules="rules" :disabled="saving" scroll-to-error>
                <el-tabs tab-position="top">
                    <el-tab-pane label="General">
                        <el-form-item label="Hostname (SMTP, IMAP)" prop="server.hostName">
                            <el-input v-model="server.hostName" :disabled="server.lockedSettings.hostName">
                                <template #prefix>
                                    <el-icon v-if="server.lockedSettings.hostName" :title="`Locked: ${server.lockedSettings.hostName}`"><Lock /></el-icon>
                                </template>
                            </el-input>
                        </el-form-item>


                        <el-form-item label="Allow Remote Connections (SMTP, IMAP)" prop="server.allowRemoteConnections">

                            <el-icon v-if="server.lockedSettings.allowRemoteConnections" :title="`Locked: ${server.lockedSettings.allowRemoteConnections}`"><Lock /></el-icon>

                            <el-switch v-model="server.allowRemoteConnections" :disabled="server.lockedSettings.allowRemoteConnections">
                            </el-switch>

                        </el-form-item>

                        <el-form-item label="Disable IPv6 (SMTP, IMAP)" prop="server.disableIPv6">
                            <el-icon v-if="server.lockedSettings.disableIPv6" :title="`Locked: ${server.lockedSettings.disableIPv6}`"><Lock /></el-icon>

                            <el-switch v-model="server.disableIPv6" :disabled="server.lockedSettings.disableIPv6" />
                        </el-form-item>

                        <el-form-item label="Require Authentication (SMTP, IMAP)" prop="server.authenticationRequired">
                            <el-icon v-if="server.lockedSettings.authenticationRequired" :title="`Locked: ${server.lockedSettings.authenticationRequired}`"><Lock /></el-icon>

                            <el-switch v-model="server.authenticationRequired" :disabled="server.lockedSettings.authenticationRequired" />
                        </el-form-item>

                        <el-form-item label="# of Messages to Keep" prop="server.numberOfMessagesToKeep">
                            <el-icon v-if="server.lockedSettings.numberOfMessagesToKeep" :title="`Locked: ${server.lockedSettings.numberOfMessagesToKeep}`"><Lock /></el-icon>

                            <el-input-number :min=1 controls-position="right" v-model="server.numberOfMessagesToKeep" :disabled="server.lockedSettings.numberOfMessagesToKeep">
                            </el-input-number>
                        </el-form-item>

                        <el-form-item label="# of Sessions to Keep" prop="server.numberOfSessionsToKeep">
                            <el-icon v-if="server.lockedSettings.numberOfSessionsToKeep" :title="`Locked: ${server.lockedSettings.numberOfSessionsToKeep}`"><Lock /></el-icon>

                            <el-input-number :min=1 controls-position="right" v-model="server.numberOfSessionsToKeep" :disabled="server.lockedSettings.numberOfSessionsToKeep" />
                        </el-form-item>

                        <el-form-item label="Require Authentication (web, API)" prop="server.webAuthenticationRequired" :rules="{validator: checkWebAuthHasUsers}">
                            <el-icon v-if="server.lockedSettings.webAuthenticationRequired" :title="`Locked: ${server.lockedSettings.webAuthenticationRequired}`"><Lock /></el-icon>

                            <el-switch v-model="server.webAuthenticationRequired" :disabled="server.lockedSettings.webAuthenticationRequired" />
                        </el-form-item>

                        <el-form-item label="Disable HTML message sanitisation on display (DANGER!)" prop="server.disableMessageSanitisation">
                            <el-icon v-if="server.lockedSettings.disableMessageSanitisation" :title="`Locked: ${server.lockedSettings.disableMessageSanitisation}`"><Lock /></el-icon>

                            <el-switch v-model="server.disableMessageSanitisation" :disabled="server.lockedSettings.disableMessageSanitisation" />
                        </el-form-item>
                    </el-tab-pane>
                    <el-tab-pane label="SMTP Server">


                        <el-form-item label="Port Number (0=auto assign)" prop="server.port">
                            <el-icon v-if="server.lockedSettings.port" :title="`Locked: ${server.lockedSettings.port}`"><Lock /></el-icon>

                            <el-input-number :min=0 :max=65535 controls-position="right" v-model="server.port" :disabled="server.lockedSettings.port" />
                        </el-form-item>

                        <el-form-item label="TLS mode" prop="server.tlsMode">

                            <el-select v-model="server.tlsMode" style="width: 100%;" :disabled="server.lockedSettings.tlsMode">
                                <el-option key="None" label="None" value="None"></el-option>
                                <el-option key="StartTls" label="STARTTLS (client requests TLS after session starts)" value="StartTls"></el-option>
                                <el-option key="ImplicitTls" label="Implicit TLS (TLS immediately)" value="ImplicitTls"></el-option>

                                <template #prefix>
                                    <el-icon v-if="server.lockedSettings.tlsMode" :title="`Locked: ${server.lockedSettings.tlsMode}`"><Lock /></el-icon>
                                </template>
                            </el-select>
                        </el-form-item>

                        <el-form-item label="Require Secure Connection" prop="server.secureConnectionRequired">
                            <el-icon v-if="server.lockedSettings.secureConnectionRequired" :title="`Locked: ${server.lockedSettings.secureConnectionRequired}`"><Lock /></el-icon>

                            <el-switch v-model="server.secureConnectionRequired" :disabled="server.lockedSettings.secureConnectionRequired" />
                        </el-form-item>

                        <el-form-item label="Auth Types when not secure connection" prop="server.smtpEnabledAuthTypesWhenNotSecureConnection">

                            <el-select v-model="server.smtpEnabledAuthTypesWhenNotSecureConnection"
                                       multiple
                                       style="width: 100%" :disabled="server.lockedSettings.smtpEnabledAuthTypesWhenNotSecureConnection">
                                <el-option v-for="item in ['ANONYMOUS', 'PLAIN', 'LOGIN', 'CRAM-MD5']"
                                           :key="item"
                                           :label="item"
                                           :value="item" />
                                <template #prefix>
                                    <el-icon v-if="server.lockedSettings.smtpEnabledAuthTypesWhenNotSecureConnection" :title="`Locked: ${server.lockedSettings.smtpEnabledAuthTypesWhenNotSecureConnection}`"><Lock /></el-icon>
                                </template>
                            </el-select>
                        </el-form-item>

                        <el-form-item label="Auth Types when secure connection" prop="server.smtpEnabledAuthTypesWhenSecureConnection">

                            <el-select v-model="server.smtpEnabledAuthTypesWhenSecureConnection"
                                       multiple
                                       style="width: 100%" :disabled="server.lockedSettings.smtpEnabledAuthTypesWhenSecureConnection">
                                <el-option v-for="item in ['ANONYMOUS', 'PLAIN', 'LOGIN', 'CRAM-MD5']"
                                           :key="item"
                                           :label="item"
                                           :value="item" />
                                <template #prefix>
                                    <el-icon v-if="server.lockedSettings.smtpEnabledAuthTypesWhenSecureConnection" :title="`Locked: ${server.lockedSettings.smtpEnabledAuthTypesWhenSecureConnection}`"><Lock /></el-icon>
                                </template>
                            </el-select>
                        </el-form-item>



                        <el-form-item label="Allow Any Credentials (off = see 'Users')" prop="server.smtpAllowAnyCredentials">
                            <el-icon v-if="server.lockedSettings.smtpAllowAnyCredentials" :title="`Locked: ${server.lockedSettings.smtpAllowAnyCredentials}`"><Lock /></el-icon>

                            <el-switch v-model="server.smtpAllowAnyCredentials" :disabled="server.lockedSettings.smtpAllowAnyCredentials" />
                        </el-form-item>

                        <el-form-item label="Credentials validation expression (see comments in appsettings.json)" prop="server.credentialsValidationExpression">

                            <el-input v-model="server.credentialsValidationExpression" :disabled="server.lockedSettings.credentialsValidationExpression">
                                <template #prefix>
                                    <el-icon v-if="server.lockedSettings.credentialsValidationExpression" :title="`Locked: ${server.lockedSettings.credentialsValidationExpression}`"><Lock /></el-icon>
                                </template>
                            </el-input>
                        </el-form-item>

                        <el-form-item label="Recipient validation expression (see comments in appsettings.json)" prop="server.recipientValidationExpression">

                            <el-input v-model="server.recipientValidationExpression" :disabled="server.lockedSettings.recipientValidationExpression">
                                <template #prefix>
                                    <el-icon v-if="server.lockedSettings.recipientValidationExpression" :title="`Locked: ${server.lockedSettings.recipientValidationExpression}`"><Lock /></el-icon>
                                </template>
                            </el-input>
                        </el-form-item>

                        <el-form-item label="Message validation expression (see comments in appsettings.json)" prop="server.messageValidationExpression">
                            <el-input v-model="server.messageValidationExpression" :disabled="server.lockedSettings.messageValidationExpression">
                                <template #prefix>
                                    <el-icon v-if="server.lockedSettings.messageValidationExpression" :title="`Locked: ${server.lockedSettings.messageValidationExpression}`"><Lock /></el-icon>
                                </template>
                            </el-input>
                        </el-form-item>
                    </el-tab-pane>


                    <el-tab-pane label="IMAP Server">
                        <el-form-item label="Port Number (0=auto assign)" prop="server.imapPort">

                            <el-icon v-if="server.lockedSettings.imapPort" :title="`Locked: ${server.lockedSettings.imapPort}`"><Lock /></el-icon>

                            <el-input-number :min=0 :max=65535 controls-position="right" v-model="server.imapPort" :disabled="server.lockedSettings.imapPort">
                            </el-input-number>
                        </el-form-item>
                    </el-tab-pane>
                    <el-tab-pane label="Message Relay">
                        <el-form-item label="Message Relay Enabled" prop="isRelayEnabled">
                            <el-icon v-if="server.lockedSettings.relaySmtpServer" :title="`Locked: ${server.lockedSettings.relaySmtpServer}`"><Lock /></el-icon>

                            <el-switch v-model="isRelayEnabled" :disabled="server.lockedSettings.relaySmtpServer" />
                        </el-form-item>

                        <el-form-item label="SMTP server" prop="server.relaySmtpServer" v-show="isRelayEnabled">

                            <el-input v-model="server.relaySmtpServer" :disabled="server.lockedSettings.relaySmtpServer">
                                <template #prefix>
                                    <el-icon v-if="server.lockedSettings.relaySmtpServer" :title="`Locked: ${server.lockedSettings.relaySmtpServer}`"><Lock /></el-icon>
                                </template>
                            </el-input>
                        </el-form-item>

                        <el-form-item label="SMTP port" prop="server.relaySmtpPort" v-show="isRelayEnabled">

                            <el-input-number :min=1 :max=65535 controls-position="right" v-model="server.relaySmtpPort" :disabled="server.lockedSettings.relaySmtpPort">
                                <template #prefix>
                                    <el-icon v-if="server.lockedSettings.relaySmtpPort" :title="`Locked: ${server.lockedSettings.relaySmtpPort}`"><Lock /></el-icon>
                                </template>
                            </el-input-number>
                        </el-form-item>

                        <el-form-item label="TLS mode" prop="server.relayTlsMode" v-show="isRelayEnabled">

                            <el-select v-model="server.relayTlsMode" :disabled="server.lockedSettings.relayTlsMode">
                                <el-option key="None" label="None" value="None"></el-option>
                                <el-option key="Auto" label="Auto" value="Auto"></el-option>
                                <el-option key="SslOnConnect" label="TLS on connect" value="SslOnConnect"></el-option>
                                <el-option key="StartTls" label="STARTTLS" value="StartTls"></el-option>
                                <el-option key="StartTlsWhenAvailable" label="STARTTLS if available" value="StartTlsWhenAvailable"></el-option>

                                <template #prefix>
                                    <el-icon v-if="server.lockedSettings.relayTlsMode" :title="`Locked: ${server.lockedSettings.relayTlsMode}`"><Lock /></el-icon>
                                </template>
                            </el-select>
                        </el-form-item>

                        <el-form-item label="Login" prop="server.relayLogin" v-show="isRelayEnabled">

                            <el-input v-model="server.relayLogin" :disabled="server.lockedSettings.relayLogin">
                                <template #prefix>
                                    <el-icon v-if="server.lockedSettings.relayLogin" :title="`Locked: ${server.lockedSettings.relayLogin}`"><Lock /></el-icon>
                                </template>
                            </el-input>
                        </el-form-item>

                        <el-form-item label="Password" prop="server.relayPassword" v-show="isRelayEnabled">

                            <el-input type="password" v-model="server.relayPassword" :disabled="server.lockedSettings.relayPassword">
                                <template #prefix>
                                    <el-icon v-if="server.lockedSettings.relayPassword" :title="`Locked: ${server.lockedSettings.relayPassword}`"><Lock /></el-icon>
                                </template>
                            </el-input>
                        </el-form-item>

                        <el-form-item label="Sender Address - leave blank to use original sender" prop="server.relaySenderAddress" v-show="isRelayEnabled">

                            <el-input v-model="server.relaySenderAddress" :disabled="server.lockedSettings.relaySenderAddress">
                                <template #prefix>
                                    <el-icon v-if="server.lockedSettings.relaySenderAddress" :title="`Locked: ${server.lockedSettings.relaySenderAddress}`"><Lock /></el-icon>
                                </template>
                            </el-input>
                        </el-form-item>

                        <el-form-item label="Auto relay expression (see comments in appsettings.json)" prop="server.relayAutomaticRelayExpression" v-show="isRelayEnabled">

                            <el-input v-model="server.relayAutomaticRelayExpression" :disabled="server.lockedSettings.relayAutomaticRelayExpression">
                                <template #prefix>
                                    <el-icon v-if="server.lockedSettings.relayAutomaticRelayExpression" :title="`Locked: ${server.lockedSettings.relayAutomaticRelayExpression}`"><Lock /></el-icon>
                                </template>
                            </el-input>
                        </el-form-item>

                        <el-form-item label="Auto-Relay Recipients" v-show="isRelayEnabled" prop="isRelayEnabled">
                            <el-icon v-if="server.lockedSettings.relayAutomaticEmails" :title="`Locked: ${server.lockedSettings.relayAutomaticEmails}`"><Lock /></el-icon>
                        </el-form-item>
                        <div v-for="(email, index) in server.relayAutomaticEmails" :key="index">
                            <el-form-item :prop="'server.relayAutomaticEmails[' + index + ']'" :rules="{required: true, message: 'Required'}">
                                <el-input v-model="server.relayAutomaticEmails[index]">
                                    <template #append>
                                        <el-button @click="server.relayAutomaticEmails.splice(index, 1)" :disabled="server.lockedSettings.relayAutomaticEmails">
                                            Remove
                                        </el-button>
                                    </template>
                                </el-input>
                            </el-form-item>
                        </div>
                        <el-button size="small" @click="server.relayAutomaticEmails.push('')" :disabled="server.lockedSettings.relayAutomaticEmails">New Auto-Relay Recipient</el-button>

                    </el-tab-pane>
                    <el-tab-pane label="Users">


                        <el-form-item>
                            Web/API, SMTP, IMAP Users:       <el-icon v-if="server.lockedSettings.users" :title="`Locked: ${server.lockedSettings.users}`"><Lock /></el-icon>

                        </el-form-item>



                        <div v-for="(user, index) in server.users" :key="index">
                            <el-form-item :prop="'server.users[' + index + ']'" :rules="{validator: checkUsernameUnique}">
                                <el-form-item label="Username" :prop="'server.users[' + index + '].username'" :rules="{required: true, message: 'Required'}">
                                    <el-input v-model="user.username" :disabled="server.lockedSettings.users">
                                    </el-input>
                                </el-form-item>
                                <el-form-item label="Password" :prop="'server.users[' + index + '].password'" :rules="{required: true, message: 'Required'}">
                                    <el-input v-model="user.password" show-password :disabled="server.lockedSettings.users">

                                    </el-input>
                                </el-form-item>
                                <el-form-item label="Default Mailbox" :prop="'server.users[' + index + '].defaultMailbox'" :rules="{required: true, message: 'Required'}">
                                    <el-select v-model="user.defaultMailbox" style="width: 150px;" :disabled="server.lockedSettings.defaultMailbox">
                                        <el-option v-for="item in [{name:'Default'}].concat(server.mailboxes)"
                                                   :key="item.name"
                                                   :label="item.name"
                                                   :value="item.name" />

                                        <template #prefix>
                                            <el-icon><MessageBox /></el-icon>
                                        </template>
                                    </el-select>
                                </el-form-item>
                                <el-button title="Remove" @click="server.users.splice(index, 1)" :disabled="server.lockedSettings.users">
                                    <el-icon><Close /></el-icon>
                                </el-button>
                            </el-form-item>
                        </div>
                        <el-button size="small" @click="server.users.push({})" :disabled="server.lockedSettings.users">New User</el-button>


                    </el-tab-pane>
                    <el-tab-pane label="Mailboxes">


                        <el-form-item>
                            Mailboxes:       <el-icon v-if="server.lockedSettings.mailboxes" :title="`Locked: ${server.lockedSettings.mailboxes}`"><Lock /></el-icon>

                        </el-form-item>



                        <div v-for="(mailbox, index) in server.mailboxes" :key="index">
                            <el-form-item :prop="'server.mailboxes[' + index + ']'" :rules="{validator: checkMailboxNameUnique}">
                                <el-button @click="server.mailboxes.splice(index, 1); server.mailboxes.splice(index-1, 0, mailbox);" :disabled="server.lockedSettings.mailboxes || index==0">
                                    <el-icon><ArrowUp /></el-icon>
                                </el-button>
                                <el-button @click="server.mailboxes.splice(index, 1); server.mailboxes.splice(index+1, 0, mailbox) " :disabled="server.lockedSettings.mailboxes || index==server.mailboxes.length-1">
                                    <el-icon><ArrowDown /></el-icon>
                                </el-button>
                                <el-form-item label="Name" :prop="'server.mailboxes[' + index + '].name'" :rules="{required: true, message: 'Required'}">
                                    <el-input v-model="mailbox.name" :disabled="server.lockedSettings.mailboxes">
                                    </el-input>
                                </el-form-item>
                                <el-form-item label="Recipients" :prop="'server.mailboxes[' + index + '].recipients'" :rules="{required: true, message: 'Required'}">
                                    <el-input v-model="mailbox.recipients" :disabled="server.lockedSettings.mailboxes">

                                    </el-input>
                                </el-form-item>
                                <el-button title="Remove" @click="server.mailboxes.splice(index, 1)" :disabled="server.lockedSettings.mailboxes">
                                    <el-icon><Close /></el-icon>
                                </el-button>
                            </el-form-item>
                        </div>
                        <el-button size="small" @click="server.mailboxes.splice(0, 0, {})" :disabled="server.lockedSettings.mailboxes">New Mailbox</el-button>


                    </el-tab-pane>
                    <el-tab-pane label="Desktop" v-if="server.isDesktopApp">



                        <el-form-item label="Minimise to system notification tray icon" prop="server.desktopMinimiseToTrayIcon">

                            <el-icon v-if="server.lockedSettings.desktopMinimiseToTrayIcon" :title="`Locked: ${server.lockedSettings.desktopMinimiseToTrayIcon}`"><Lock /></el-icon>

                            <el-switch v-model="server.desktopMinimiseToTrayIcon" :disabled="server.lockedSettings.desktopMinimiseToTrayIcon">
                            </el-switch>

                        </el-form-item>
                    </el-tab-pane>
                </el-tabs>
            </el-form>
        </div>

        <template #footer>
            <span class="dialog-footer">
                <el-button @click="save" :disabled="saving" :loading="saving">Save</el-button>
            </span>
        </template>
    </el-dialog>
</template>

<script lang="ts">

    import { FormInstance } from 'element-plus'
    import { Component, Vue, Prop, Watch, toNative, Emit } from "vue-facing-decorator";
    import HubConnectionManager from "../HubConnectionManager";
    import { Mutex } from "async-mutex";
    import ServerController from "../ApiClient/ServerController";
    import Server from "../ApiClient/Server";

    @Component
    class SettingsDialog extends Vue {

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
                    port: [{ required: true, message: 'Port number is required' }],
                    numberOfMessagesToKeep: [{ required: true, message: '# of Messages to Keep is required' }],
                    numberOfSessionsToKeep: [{ required: true, message: '# of Sessions to Keep is required' }],
                    relaySmtpServer: [] as object[],
                    relaySmtpPort: [] as object[]
                }
            };

            if (this.isRelayEnabled) {
                result.server.relaySmtpServer.push({ required: true, message: "SMTP server is required if relaying enabled" });
                result.server.relaySmtpPort.push({ required: true, message: "SMTP port is required if relaying enabled" });
            }

            return result;

        }

        checkMailboxNameUnique(rule: any, value: any, callback: any) {
            if (value.name === "Default") {
                callback(new Error("Name cannot be 'Default'"));
            }
            else if (value && this.server?.mailboxes.filter(u => u != value).find(u => u.name == value.name)) {
                callback(new Error('Name must be unique'));
            } else {
                callback();
            }

        }

        checkUsernameUnique(rule: any, value: any, callback: any) {
            if (value && this.server?.users.filter(u => u != value).find(u => u.username == value.username)) {
                callback(new Error('Username must be unique'));
            } else {
                callback();
            }

        }

        checkWebAuthHasUsers(rule: any, value: any, callback: any) {
            if (value && !this.server?.users?.length) {
                callback("You must add at least one user under 'Users' to enable this setting.")
            } else {
                callback();
            }
        }

        @Prop()
        visible: boolean = false;

        @Emit("closed")
        handleClose() {
            return;
        }


        private isRelayEnabledValue: boolean = false;
        get isRelayEnabled() {
            return this.isRelayEnabledValue;
        }

        set isRelayEnabled(value: boolean) {
            this.isRelayEnabledValue = value;
            if (!this.isRelayEnabledValue && this.server) {
                this.server.relaySmtpServer = "";
                this.server.relayAutomaticEmails.splice(0, this.server.relayAutomaticEmails.length);
            }
        }

        async save() {
            this.saving = true;
            this.error = null;
            try {
                let valid = await (this.$refs["form"] as FormInstance).validate()

                if (valid) {

                    await new ServerController().updateServer(this.server!);
                    this.handleClose();
                }
            } catch (e: any) {
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
                this.isRelayEnabled = !!this.server.relaySmtpServer;
            } catch (e: any) {
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

    export default toNative(SettingsDialog)
</script>