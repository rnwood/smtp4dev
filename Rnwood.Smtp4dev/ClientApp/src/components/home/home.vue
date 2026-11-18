<template>
    <el-container class="fill hfillpanel">
        <el-header height="35" style="display:flex; flex-wrap:wrap; align-items: end; column-gap: 12px;">
            <div style="flex: 0 0 content">
                <a href="https://github.com/rnwood/smtp4dev/" target="_blank">

                    <UseDark v-slot="{ isDark }">
                        <img height="35" :src="isDark ? './logo-dark.png' : './logo.png'" class="logo" alt="smtp4dev" />
                    </UseDark>
                </a>
            </div>

            <VersionInfo style="flex: 1 1 content; align-self: center;"></VersionInfo>

            <UseDark v-slot="{ isDark, toggleDark }">
                <el-button style="font-size: 1.7em; padding: 6px;" circle :icon="isDark ? 'sunny' : 'moon'" @click="toggleDark()" />
            </UseDark>

            <hubconnstatus style="flex: 1 1 content" :connection="connection"></hubconnstatus>
            <serverstatus style="flex: 0 1 content" :connection="connection" v-on:showsettings="showSettings(true)">
            </serverstatus>
            <el-dropdown style="flex: 0 0 content;" trigger="click" @command="handleMenuCommand">
                <el-button style="font-size: 1.7em; padding: 6px;" circle icon="more" />

                <template #dropdown>
                    <el-dropdown-menu>
                        <el-dropdown-item icon="Connection" command="api">API</el-dropdown-item>
                        <el-dropdown-item icon="QuestionFilled" command="help">Help</el-dropdown-item>
                        <el-dropdown-item icon="Service" command="issues">Issues</el-dropdown-item>
                    </el-dropdown-menu>
                </template>
            </el-dropdown>
        </el-header>
        <settingsdialog v-model="settingsVisible" :connection="connection" v-on:closed="showSettings(false)" />
        <el-main class="fill vfillpanel">
            <el-tabs id="maintabs" class="fill" v-model="activeTabId" type="border-card">
                <el-tab-pane label="Messages" name="messages" class="vfillpanel">
                    <template #label>
                        <span>
                            <el-icon><message /></el-icon> Messages
                        </span>
                    </template>

                    <splitpanes class="default-theme fill" @resize="messageListPaneSize = $event[0].size">

                        <pane class="hfillpanel" :size="messageListPaneSize">
                            <messagelist class="fill" @selected-message-changed="selectedMessageChanged"
                                         :connection="connection" />
                        </pane>
                        <pane class="hfillpanel" :size="100 - messageListPaneSize">
                            <messageview class="fill" :connection="connection" v-bind:message-summary="selectedMessage" />
                        </pane>
                    </splitpanes>
                </el-tab-pane>

                <el-tab-pane label="Sessions" name="sessions" class="vfillpanel">
                    <template #label>
                        <span>
                            <el-icon><monitor /></el-icon> Sessions
                        </span>
                    </template>


                    <splitpanes class="default-theme fill" @resize="sessionListPaneSize = $event[0].size">

                        <pane class="vfillpanel" :size="sessionListPaneSize">
                            <sessionlist class="fill" @selected-session-changed="selectedSessionChanged"
                                         :connection="connection" />
                        </pane>

                        <pane class="vfillpanel" :size="100 - sessionListPaneSize">
                            <sessionview class="fill" v-bind:session-summary="selectedSession" />
                        </pane>
                    </splitpanes>
                </el-tab-pane>

                <el-tab-pane v-if="hasSmsGateway" label="SMS" name="sms" class="vfillpanel">
                    <template #label>
                        <span data-testid="tab-sms">
                            <el-icon><chat-dot-square /></el-icon> SMS
                        </span>
                    </template>

                    <splitpanes class="default-theme fill" @resize="smsListPaneSize = $event[0].size">

                        <pane class="hfillpanel" :size="smsListPaneSize">
                            <smslist ref="smslist" class="fill" @selected-sms-changed="selectedSmsChanged"
                                     :connection="connection" />
                        </pane>
                        <pane class="hfillpanel" :size="100 - smsListPaneSize">
                            <smsview class="fill" :sms="selectedSms" @deleted="gatewayItemDeleted('smslist')" />
                        </pane>
                    </splitpanes>
                </el-tab-pane>

                <el-tab-pane v-if="hasTargitoGateway" label="Targito" name="targito" class="vfillpanel">
                    <template #label>
                        <span data-testid="tab-targito">
                            <el-icon><promotion /></el-icon> Targito
                        </span>
                    </template>

                    <splitpanes class="default-theme fill" @resize="targitoListPaneSize = $event[0].size">

                        <pane class="hfillpanel" :size="targitoListPaneSize">
                            <targitolist ref="targitolist" class="fill" @selected-targito-changed="selectedTargitoChanged"
                                         :connection="connection" />
                        </pane>
                        <pane class="hfillpanel" :size="100 - targitoListPaneSize">
                            <targitoview class="fill" :item="selectedTargito" @deleted="gatewayItemDeleted('targitolist')" />
                        </pane>
                    </splitpanes>
                </el-tab-pane>

                <el-tab-pane label="Server Log" name="serverlog" class="vfillpanel">
                    <template #label>
                        <span>
                            <el-icon><document /></el-icon> Server Log
                        </span>
                    </template>
                    <serverlog class="fill" :connection="connection" />
                </el-tab-pane>
            </el-tabs>
        </el-main>
    </el-container>
</template>


<script lang="ts">


    import { UseDark } from '@vueuse/components'

    import { Component, Vue, toNative } from "vue-facing-decorator";
    import MessageSummary from "../../ApiClient/MessageSummary";
    import SessionSummary from "../../ApiClient/SessionSummary";
    import MessageList from "@/components/messagelist.vue";
    import MessageView from "@/components/messageview.vue";
    import SessionList from "@/components/sessionlist.vue";
    import SessionView from "@/components/sessionview.vue";
    import ServerLog from "@/components/serverlog.vue";
    import SmsList from "@/components/fakegateways/smslist.vue";
    import SmsView from "@/components/fakegateways/smsview.vue";
    import TargitoList from "@/components/fakegateways/targitolist.vue";
    import TargitoView from "@/components/fakegateways/targitoview.vue";
    import FakeGatewaysController, { SmsMessage, TargitoRecord } from "../../ApiClient/FakeGatewaysController";
    import VersionInfo from "@/components/versionInfo.vue";
    import HubConnectionManager from "@/HubConnectionManager";
    import ServerStatus from "@/components/serverstatus.vue";
    import SettingsDialog from "@/components/settingsdialog.vue";
    import HubConnectionStatus from "@/components/hubconnectionstatus.vue";
    import { Splitpanes, Pane } from 'splitpanes';
    import 'splitpanes/dist/splitpanes.css';
    import { ElIcon } from "element-plus";
    import { Watch } from "vue-facing-decorator";

    @Component({
        components: {
            messagelist: MessageList,
            messageview: MessageView,
            sessionlist: SessionList,
            sessionview: SessionView,
            serverlog: ServerLog,
            smslist: SmsList,
            smsview: SmsView,
            targitolist: TargitoList,
            targitoview: TargitoView,
            hubconnstatus: HubConnectionStatus,
            serverstatus: ServerStatus,
            settingsdialog: SettingsDialog,
            splitpanes: Splitpanes,
            pane: Pane,
            VersionInfo,
            UseDark
        }
    })
    class Home extends Vue {
        activeTabId = "messages";
        selectedMessage: MessageSummary | null = null;
        selectedSession: SessionSummary | null = null;
        selectedSms: SmsMessage | null = null;
        selectedTargito: TargitoRecord | null = null;
        gatewayFeatures: string[] = [];
        gatewayHealthLoaded = false;
        suppressRouteUpdate = false;

        connection: HubConnectionManager | null = null;

        settingsVisible: boolean = false;

        showSettings(visible: boolean) {
            this.settingsVisible = visible;
        }

        handleMenuCommand(command: string) {
            switch (command) {
                case "api":
                    window.open("/api");
                    break;
                case "help":
                    window.open("https://github.com/rnwood/smtp4dev/blob/master/docs/README.md");
                    break;
                case "issues":
                    window.open("https://github.com/rnwood/smtp4dev/issues");
                    break;

                default:
                    break;
            }
        }

        selectedMessageChanged(selectedMessage: MessageSummary | null) {
            this.selectedMessage = selectedMessage;
        }


        selectedSessionChanged(selectedSession: SessionSummary | null) {
            this.selectedSession = selectedSession;
            
            // Update URL when session is selected
            if (!this.suppressRouteUpdate && this.activeTabId === 'sessions') {
                if (selectedSession) {
                    this.$router.push({ path: `/sessions/session/${selectedSession.id}` });
                } else {
                    this.$router.push({ path: '/sessions' });
                }
            }
        }

        get hasSmsGateway(): boolean {
            return this.gatewayFeatures.includes("konzulta");
        }

        get hasTargitoGateway(): boolean {
            return this.gatewayFeatures.includes("targito");
        }

        selectedSmsChanged(selectedSms: SmsMessage | null, routeDriven: boolean = false) {
            this.selectedSms = selectedSms;
            if (routeDriven) {
                return;
            }

            // Selections that merely follow the URL (routeDriven) are not written back, so a deep link
            // to an item that has not been loaded (or does not exist) keeps its URL.
            if (!this.suppressRouteUpdate && this.activeTabId === 'sms') {
                this.$router.push({ path: selectedSms ? `/sms/${encodeURIComponent(selectedSms.id)}` : '/sms' });
            }
        }

        selectedTargitoChanged(selectedTargito: TargitoRecord | null, routeDriven: boolean = false) {
            this.selectedTargito = selectedTargito;
            if (routeDriven) {
                return;
            }

            if (!this.suppressRouteUpdate && this.activeTabId === 'targito') {
                this.$router.push({ path: selectedTargito ? `/targito/${encodeURIComponent(selectedTargito.id)}` : '/targito' });
            }
        }

        gatewayItemDeleted(listRef: string) {
            (this.$refs[listRef] as any)?.refresh?.(true);
        }

        @Watch("activeTabId")
        onActiveTabChanged(newTab: string) {
            // Update URL when tab changes
            if (!this.suppressRouteUpdate) {
                switch (newTab) {
                    case 'messages':
                        if (this.selectedMessage) {
                            // If a message is selected, let messagelist component handle the specific route
                            // But we need to ensure we navigate to messages first
                            if (!this.$route.path.startsWith('/messages/')) {
                                this.$router.push({ path: '/messages' });
                            }
                        } else {
                            // Navigate to base messages route if no message selected
                            if (!this.$route.path.startsWith('/messages')) {
                                this.$router.push({ path: '/messages' });
                            }
                        }
                        break;
                    case 'sessions':
                        if (this.selectedSession) {
                            this.$router.push({ path: `/sessions/session/${this.selectedSession.id}` });
                        } else {
                            // Don't override a more specific route with a less specific one
                            // This prevents clearing the session ID from the URL when the session
                            // hasn't been loaded yet on initial page load
                            if (!this.$route.path.startsWith('/sessions/session/')) {
                                this.$router.push({ path: '/sessions' });
                            }
                        }
                        break;
                    case 'sms':
                        if (this.selectedSms) {
                            this.$router.push({ path: `/sms/${encodeURIComponent(this.selectedSms.id)}` });
                        } else if (!this.$route.path.startsWith('/sms/')) {
                            // Keep the id of a deep link that the list has not selected yet
                            this.$router.push({ path: '/sms' });
                        }
                        break;
                    case 'targito':
                        if (this.selectedTargito) {
                            this.$router.push({ path: `/targito/${encodeURIComponent(this.selectedTargito.id)}` });
                        } else if (!this.$route.path.startsWith('/targito/')) {
                            this.$router.push({ path: '/targito' });
                        }
                        break;
                    case 'serverlog':
                        this.$router.push({ path: '/serverlog' });
                        break;
                }
            }
        }

        @Watch("$route")
        onRouteChanged() {
            // Update active tab based on route
            const path = this.$route.path;
            this.suppressRouteUpdate = true;
            
            if (path.startsWith('/messages') || path === '/') {
                this.activeTabId = 'messages';
            } else if (path.startsWith('/sessions')) {
                this.activeTabId = 'sessions';
            } else if (path.startsWith('/sms')) {
                // Until the health check has answered the tab does not exist yet. mounted() re-evaluates the route.
                if (this.hasSmsGateway) {
                    this.activeTabId = 'sms';
                } else if (this.gatewayHealthLoaded) {
                    this.activeTabId = 'messages';
                    this.$router.replace('/messages');
                }
            } else if (path.startsWith('/targito')) {
                if (this.hasTargitoGateway) {
                    this.activeTabId = 'targito';
                } else if (this.gatewayHealthLoaded) {
                    this.activeTabId = 'messages';
                    this.$router.replace('/messages');
                }
            } else if (path.startsWith('/serverlog')) {
                this.activeTabId = 'serverlog';
            }
            
            this.suppressRouteUpdate = false;
        }

        get messageListPaneSize(): number {

            var storedValue = window.localStorage.getItem("messagelist-panelsize");
            if (storedValue) {
                return Number(storedValue);
            }

            return 40;
        }

        set messageListPaneSize(value: number) {
            window.localStorage.setItem("messagelist-panelsize", value.toString());
        }

        get sessionListPaneSize(): number {

            var storedValue = window.localStorage.getItem("sessionlist-panelsize");
            if (storedValue) {
                return Number(storedValue);
            }

            return 40;
        }

        set sessionListPaneSize(value: number) {
            window.localStorage.setItem("sessionlist-panelsize", value.toString());
        }

        get smsListPaneSize(): number {

            var storedValue = window.localStorage.getItem("smslist-panelsize");
            if (storedValue) {
                return Number(storedValue);
            }

            return 40;
        }

        set smsListPaneSize(value: number) {
            window.localStorage.setItem("smslist-panelsize", value.toString());
        }

        get targitoListPaneSize(): number {

            var storedValue = window.localStorage.getItem("targitolist-panelsize");
            if (storedValue) {
                return Number(storedValue);
            }

            return 40;
        }

        set targitoListPaneSize(value: number) {
            window.localStorage.setItem("targitolist-panelsize", value.toString());
        }

        async loadGatewayFeatures() {
            try {
                const health = await new FakeGatewaysController().getHealth();
                this.gatewayFeatures = health.features || [];
            } catch {
                this.gatewayFeatures = [];
            } finally {
                this.gatewayHealthLoaded = true;
            }
        }

        async mounted() {
            this.connection = new HubConnectionManager("hubs/notifications")
            this.connection.start();
            
            // Initialize tab from route on mount BEFORE data loads
            this.onRouteChanged();

            await this.loadGatewayFeatures();
            // Now the SMS / Targito tabs exist, so a deep link to them can be honoured
            this.onRouteChanged();
        }

        destroyed() {
            if (this.connection) {
                this.connection.stop();
            }
        }

    }

    export default toNative(Home);
</script>