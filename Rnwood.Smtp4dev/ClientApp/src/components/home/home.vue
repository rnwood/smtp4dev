<template>
    <el-container class="fill hfillpanel">
        <el-header height="35">
            <h1>
                <a href="https://github.com/rnwood/smtp4dev/" target="_blank">
                    <img height="35" src="logo.png" alt="smtp4dev" />
                </a>
            </h1>
            <hubconnstatus style="float:right" :connection="connection"></hubconnstatus>
            <serverstatus style="float:right" v-show="connection && connection.connected" :connection="connection"></serverstatus>
        </el-header>
        <el-main class="fill vfillpanel">
            <el-tabs id="maintabs" class="fill" value="messages" type="border-card">
                <el-tab-pane label="Messages" name="messages" class="vfillpanel">
                    <span slot="label">
                        <i class="el-icon-message"></i> Messages
                    </span>

                    <div class="fillhalf vfillpanel">
                        <messagelist class="fill" @selected-message-changed="selectedMessageChanged" :connection="connection" />
                    </div>
                    <div class="fillhalf vfillpanel">
                        <messageview class="fill" v-bind:message-summary="selectedMessage" />
                    </div>
                </el-tab-pane>

                <el-tab-pane label="Sessions" name="sessions" class="vfillpanel">
                    <span slot="label">
                        <i class="el-icon-monitor"></i> Sessions
                    </span>

                    <div class="fillhalf vfillpanel">
                        <sessionlist class="fill" @selected-session-changed="selectedSessionChanged" :connection="connection" />
                    </div>

                    <div class="fillhalf vfillpanel">
                        <sessionview class="fill" v-bind:session-summary="selectedSession" />
                    </div>
                </el-tab-pane>
            </el-tabs>
        </el-main>
    </el-container>
</template>


<script lang="ts">
    import Vue from "vue";
    import { Component } from "vue-property-decorator";
    import MessageSummary from "../../ApiClient/MessageSummary";
    import SessionSummary from "../../ApiClient/SessionSummary";
    import MessageList from "@/components/messagelist.vue";
    import MessageView from "@/components/messageview.vue";
    import SessionList from "@/components/sessionlist.vue";
    import SessionView from "@/components/sessionview.vue";
    import HubConnectionManager from "@/HubConnectionManager";
    import ServerStatus from "@/components/serverstatus.vue";
    import HubConnectionStatus from "@/components/hubconnectionstatus.vue";

    @Component({
        components: {
            messagelist: MessageList,
            messageview: MessageView,
            sessionlist: SessionList,
            sessionview: SessionView,
            hubconnstatus: HubConnectionStatus,
            serverstatus: ServerStatus
        }
    })
    export default class Home extends Vue {
        selectedMessage: MessageSummary | null = null;
        selectedSession: SessionSummary | null = null;

        connection: HubConnectionManager | null = null;

        selectedMessageChanged(selectedMessage: MessageSummary | null) {
            this.selectedMessage = selectedMessage;
        }

        selectedSessionChanged(selectedSession: SessionSummary | null) {
            this.selectedSession = selectedSession;
        }

        constructor() {
            super();
        }

        async mounted() {
            this.connection = new HubConnectionManager("/hubs/notifications")
            this.connection.start();
        }

        destroyed() {
            if (this.connection) {
                this.connection.stop();
            }
        }
    }
</script>