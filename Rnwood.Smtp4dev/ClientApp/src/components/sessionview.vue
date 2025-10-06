<template>
    <div class="sessionview fill vfillpanel" v-loading.body="loading">
        <el-alert v-if="error" type="error">
            {{error.message}}
            <el-button v-on:click="loadMessage">Retry</el-button>
        </el-alert>
        <div v-if="error" class="hfillpanel fill">&nbsp;</div>

        <div v-if="sessionSummary" class="hfillpanel fill">
            <el-alert v-if="session && session.error"
                      type="warning"
                      show-icon
                      title="This session terminated abnormally">{{session.error}}</el-alert>
                <el-alert v-for="warning in session.warnings"
                          v-bind:key="warning.details"
                          :title="'Warning: ' + warning.details"
                          type="warning"
                          show-icon />

            <el-tabs value="log" style="height: 100%; width:100%" class="fill" type="border-card">
                <el-tab-pane id="log" class="hfillpanel">
                    <template #label>
                        <span>
                            <i class="notebook-2"></i> Log
                        </span>
                    </template>
                    <div class="toolbar">
                        <el-button size="small" @click="download">Open</el-button>
                    </div>

                    <div v-show="log" class="vfillpanel fill">
                        <textview :text="log" class="fill"></textview>
                    </div>
                </el-tab-pane>
            </el-tabs>
        </div>

        <div v-if="!sessionSummary" class="fill nodetails centrecontents">
            <div>No session selected</div>
        </div>
    </div>
</template>
<script lang="ts">
    import { Component, Vue, Prop, Watch, toNative, Inject } from "vue-facing-decorator";

    import SessionsController from "../ApiClient/SessionsController";
    import SessionSummary from "../ApiClient/SessionSummary";
    import Session from "../ApiClient/Session";
    import TextView from "@/components/textview.vue";
    import HubConnectionManager from "../HubConnectionManager";

    @Component({
        components: {
            textview: TextView
        }
    })
    class SessionView extends Vue {

        @Prop({})
        sessionSummary: SessionSummary | null = null;
        
        @Inject({ default: null })
        connection!: HubConnectionManager | null;
        
        session: Session | null = null;
        log: string | null = null;

        error: Error | null = null;
        loading = false;
        
        private pollInterval: number | null = null;

        @Watch("sessionSummary")
        async onMessageChanged(
            value: SessionSummary | null,
            oldValue: SessionSummary | null
        ) {
            await this.loadSession();
            this.setupPolling();
        }

        async loadMessage() {
            console.warn('not implemented');
        }

        download() {
            if (this.sessionSummary) {
                window.open(
                    new SessionsController().getSessionLog_url(this.sessionSummary.id)
                );
            }
        }

        async loadSession() {
            this.error = null;
            this.loading = true;
            this.session = null;
            this.log = null;

            try {
                if (this.sessionSummary != null) {
                    this.session = await new SessionsController().getSession(
                        this.sessionSummary.id
                    );
                    this.log = await new SessionsController().getSessionLog(
                        this.sessionSummary.id
                    );
                }
            } catch (e: any) {
                this.error = e;
            } finally {
                this.loading = false;
            }
        }
        
        async refreshLog() {
            if (this.sessionSummary != null && !this.sessionSummary.endDate) {
                try {
                    const newSession = await new SessionsController().getSession(
                        this.sessionSummary.id
                    );
                    const newLog = await new SessionsController().getSessionLog(
                        this.sessionSummary.id
                    );
                    
                    // Only update if content has changed
                    if (this.log !== newLog) {
                        this.log = newLog;
                    }
                    
                    // Update session properties if they've changed
                    if (JSON.stringify(this.session) !== JSON.stringify(newSession)) {
                        this.session = newSession;
                    }
                    
                    // If session has ended, stop polling
                    if (newSession) {
                        // Session ended, need to update the summary as well
                        this.stopPolling();
                    }
                } catch (e: any) {
                    // Silently ignore errors during polling
                    console.warn('Failed to refresh session log:', e);
                }
            }
        }
        
        setupPolling() {
            this.stopPolling();
            
            // Only poll if session is active (no end date)
            if (this.sessionSummary && !this.sessionSummary.endDate) {
                this.pollInterval = window.setInterval(() => {
                    this.refreshLog();
                }, 1000); // Poll every second
            }
        }
        
        stopPolling() {
            if (this.pollInterval !== null) {
                clearInterval(this.pollInterval);
                this.pollInterval = null;
            }
        }
        
        onSessionUpdated(sessionId: string) {
            if (this.sessionSummary && this.sessionSummary.id === sessionId) {
                this.refreshLog();
            }
        }

        async created() {
            if (this.connection) {
                this.connection.on("sessionupdated", this.onSessionUpdated.bind(this));
            }
        }

        async unmounted() {
            this.stopPolling();
        }
    }


    export default toNative(SessionView)
</script>