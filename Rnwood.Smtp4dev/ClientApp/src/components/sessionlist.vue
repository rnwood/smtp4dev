<template>
  <div class="sessionlist">
    <div class="toolbar">
      <el-button-group>
        <confirmationdialog
          v-on:confirm="clear"
          always-key="deleteAllSessions"
          message="Are you sure you want to delete all sessions?"
        >
          <el-button size="small" type="primary" icon="el-icon-close">Clear</el-button>
        </confirmationdialog>
        <el-button
          size="small"
          type="primary"
          icon="el-icon-delete"
          v-on:click="deleteSelected"
          :disabled="!selectedsession"
        >Delete</el-button>
        <el-button
          size="small"
          type="primary"
          icon="el-icon-refresh"
          v-on:click="refresh"
          :disabled="loading"
        >Refresh</el-button>
      </el-button-group>

      <hubconnstatus style="float:right" :connection="connection"></hubconnstatus>
    </div>

    <el-alert v-if="error" type="error" title="Error" show-icon>
      {{error.message}}
      <el-button v-on:click="refresh">Retry</el-button>
    </el-alert>
    <el-table
      :data="sessions"
      v-loading="loading"
      empty-text="No sessions"
      highlight-current-row
      @current-change="handleCurrentChange"
      class="table"
      :default-sort="{prop: 'endDate', order: 'descending'}"
      type="selection"
      reserve-selection="true"
      row-key="id"
      stripe
    >
      <el-table-column
        property="endDate"
        label="End Date"
        width="120"
        sortable
        :formatter="formatDate"
      ></el-table-column>
      <el-table-column property="clientAddress" label="Client Address" sortable></el-table-column>
      <el-table-column property="numberOfMessages" label="# Msgs" width="120" sortable>
        <template slot-scope="scope">
          <i
            class="el-icon-warning"
            v-if="scope.row.terminatedWithError"
            title="This session terminated abnormally"
          ></i>
          {{scope.row.numberOfMessages}}
        </template>
      </el-table-column>
    </el-table>
  </div>
</template>
<script lang="ts">
import { Component } from "vue-property-decorator";
import Vue from "vue";
import SessionsController from "../ApiClient/SessionsController";
import SessionSummary from "../ApiClient/SessionSummary";
import * as moment from 'moment';
import HubConnectionManager from "../HubConnectionManager";
import sortedArraySync from "../sortedArraySync";
import { Mutex } from "async-mutex";
import HubConnectionStatus from "@/components/hubconnectionstatus.vue"
import ConfirmationDialog from "@/components/confirmationdialog.vue"

@Component({
  components: {
    hubconnstatus: HubConnectionStatus,
    confirmationdialog: ConfirmationDialog
  }
})
export default class SessionList extends Vue {
  constructor() {
    super();

    this.connection = new HubConnectionManager("hubs/sessions", this.refresh);
    this.connection.on("sessionschanged", () => {
      this.refresh();
    });
    this.connection.start();
  }

  connection: HubConnectionManager;

  sessions: SessionSummary[] = [];
  error: Error | null = null;
  selectedsession: SessionSummary | null = null;
  loading = false;
  private mutex = new Mutex();

  handleCurrentChange(session: SessionSummary | null): void {
    this.selectedsession = session;
    this.$emit("selected-session-changed", session);
  }

  formatDate(
    row: number,
    column: number,
    cellValue: Date,
    index: number
  ): string {
    return (<any> moment)(cellValue).format("YYYY-MM-DD HH:mm:ss");
  }

  async deleteSelected() {
    if (this.selectedsession == null) {
      return;
    }

    try {
      await new SessionsController().delete(this.selectedsession.id);
      this.refresh();
    } catch (e) {
      this.error = e;
    }
  }

  async clear() {
    try {
      await new SessionsController().deleteAll();
      this.refresh();
    } catch (e) {
      this.error = e;
    }
  }

  refresh = async () => {
    var unlock = await this.mutex.acquire();

    try {
      this.error = null;
      this.loading = true;

      var newSessions = await new SessionsController().getSummaries();
      sortedArraySync(
        newSessions,
        this.sessions,
        (a: SessionSummary, b: SessionSummary) => a.id == b.id
      );
    } catch (e) {
      this.error = e;
    } finally {
      this.loading = false;
      unlock();
    }
  };

  async created() {
    this.refresh();
  }

  async destroyed() {
    this.connection.stop();
  }
}
</script>