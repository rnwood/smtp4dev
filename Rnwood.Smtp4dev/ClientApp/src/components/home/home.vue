<template>
    <el-container class="fill hfillpanel">
        <el-header height="35" style="display:flex; align-items: stretch; column-gap: 12px;">
            <h1 style="flex: 0 0 content">
                <a href="https://github.com/rnwood/smtp4dev/" target="_blank">
                    <img height="35" src="logo.png" alt="smtp4dev" />
                </a>
            </h1>

            <VersionInfo style="flex: 1 1 content; align-self: center;"></VersionInfo>

            <hubconnstatus style="flex: 0 1 content" :connection="connection"></hubconnstatus>
            <serverstatus style="flex: 0 1 content" :connection="connection" v-on:showsettings="settingsVisible = true">
            </serverstatus>
            <el-dropdown trigger="click" @command="handleMenuCommand">
                <el-button style="flex: 0 0 content; font-size: 1.7em; padding: 6px;" circle icon="more" />

                <template #dropdown>
                    <el-dropdown-menu>
                        <el-dropdown-item icon="Connection" command="api">API</el-dropdown-item>
                    </el-dropdown-menu>
                </template>
            </el-dropdown>
        </el-header>
        <settingsdialog :visible="settingsVisible" :connection="connection" v-on:closed="settingsVisible = false" />
        <el-main class="fill vfillpanel">
            <el-tabs id="maintabs" class="fill" :value="'messages'" type="border-card" >
                <el-tab-pane label="Messages" name="messages" class="vfillpanel">
                    <template #label>
                        <span>
                            <i class="message"></i> Messages
                        </span>
                    </template>

                    <splitpanes class="default-theme fill" @resize="messageListPaneSize = $event[0].size">

                        <pane class="hfillpanel" :size="messageListPaneSize">
                            <messagelist class="fill" @selected-message-changed="selectedMessageChanged"
                                         :connection="connection" />
                        </pane>
                        <pane class="hfillpanel" :size="100 - messageListPaneSize">
                            <messageview class="fill" v-bind:message-summary="selectedMessage" />
                        </pane>
                    </splitpanes>
                </el-tab-pane>

                <el-tab-pane label="Sessions" name="sessions" class="vfillpanel">
                    <template #label>
                        <span>
                            <i class="monitor"></i> Sessions
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
            </el-tabs>
        </el-main>
    </el-container>
</template>


<script lang="ts">

    import { Component, Vue, toNative } from "vue-facing-decorator";
    import MessageSummary from "../../ApiClient/MessageSummary";
    import SessionSummary from "../../ApiClient/SessionSummary";
    import MessageList from "@/components/messagelist.vue";
    import MessageView from "@/components/messageview.vue";
    import SessionList from "@/components/sessionlist.vue";
    import SessionView from "@/components/sessionview.vue";
    import VersionInfo from "@/components/versionInfo.vue";
    import HubConnectionManager from "@/HubConnectionManager";
    import ServerStatus from "@/components/serverstatus.vue";
    import SettingsDialog from "@/components/settingsdialog.vue";
    import HubConnectionStatus from "@/components/hubconnectionstatus.vue";
    import { Splitpanes, Pane } from 'splitpanes';
    import 'splitpanes/dist/splitpanes.css';
    import { ElIcon } from "element-plus";

    @Component({
        components: {
            messagelist: MessageList,
            messageview: MessageView,
            sessionlist: SessionList,
            sessionview: SessionView,
            hubconnstatus: HubConnectionStatus,
            serverstatus: ServerStatus,
            settingsdialog: SettingsDialog,
            splitpanes: Splitpanes,
            pane: Pane,
            VersionInfo
        }
    })
    class Home extends Vue {
        selectedMessage: MessageSummary | null = null;
        selectedSession: SessionSummary | null = null;

        connection: HubConnectionManager | null = null;

        settingsVisible: boolean = false;

        handleMenuCommand(command: string) {
            switch (command) {
                case "api":
                    window.open("/api");
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

        async mounted() {
            this.connection = new HubConnectionManager("hubs/notifications")
            this.connection.start();
        }

        destroyed() {
            if (this.connection) {
                this.connection.stop();
            }
        }

    }

    export default toNative(Home);
</script>