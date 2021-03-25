<template>
  <div class="sessionview fill vfillpanel" v-loading.body="loading">
    <el-alert v-if="error" type="error">
      {{error.message}}
      <el-button v-on:click="loadMessage">Retry</el-button>
    </el-alert>
    <div v-if="error" class="hfillpanel fill">&nbsp;</div>

    <div v-if="sessionSummary" class="hfillpanel fill">
      <el-alert
        v-if="session && session.error"
        type="warning"
        show-icon
        title="This session terminated abnormally"
      >{{session.error}}</el-alert>

      <el-tabs value="log" style="height: 100%; width:100%" class="fill" type="border-card">
        <el-tab-pane name="log" class="hfillpanel">
          <span slot="label">
            <i class="el-icon-notebook-2"></i> Log
          </span>
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
import { Component, Prop, Watch } from "vue-property-decorator";
import Vue from "vue";
import SessionsController from "../ApiClient/SessionsController";
import SessionSummary from "../ApiClient/SessionSummary";
import Session from "../ApiClient/Session";
import TextView from "@/components/textview.vue";

@Component({
  components: {
    textview: TextView
  }
})
export default class SessionView extends Vue {
  constructor() {
    super();
  }

  @Prop({})
  sessionSummary: SessionSummary | null = null;
  session: Session | null = null;
  log: string | null = null;

  error: Error | null = null;
  loading = false;

  @Watch("sessionSummary")
  async onMessageChanged(
    value: SessionSummary | null,
    oldValue: SessionSummary | null
  ) {
    await this.loadSession();
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
    } catch (e) {
      this.error = e;
    } finally {
      this.loading = false;
    }
  }

  async created() {}

  async destroyed() {}
}
</script>