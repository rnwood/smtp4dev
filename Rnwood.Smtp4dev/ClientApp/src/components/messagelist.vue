<template>
  <div class="messagelist">
    <div class="toolbar">
      <el-button-group>
        <confirmationdialog
          v-on:confirm="clear"
          always-key="deleteAllMessages"
          message="Are you sure you want to delete all messages?"
        >
          <el-button size="small" type="primary" icon="el-icon-close">Clear</el-button>
        </confirmationdialog>

        <el-button
          size="small"
          type="primary"
          icon="el-icon-delete"
          v-on:click="deleteSelected"
          :disabled="!selectedmessage"
        >Delete</el-button>
        <el-button
          size="small"
          type="primary"
          icon="el-icon-refresh"
          v-on:click="refresh"
          :disabled="loading"
        >Refresh</el-button>
      </el-button-group>

      <el-input
        v-model="searchTerm"
        size="small"
        clearable
        placeholder="Search"
        prefix-icon="el-icon-search"
        style="width: 35%; min-width: 150px;"
      />

      <hubconnstatus style="float:right" :connection="connection"></hubconnstatus>
    </div>

    <el-alert v-if="error" type="error" title="Error" show-icon>
      {{error.message}}
      <el-button v-on:click="refresh">Retry</el-button>
    </el-alert>

    <el-table
      :data="filteredMessages"
      v-loading="loading"
      :empty-text="emptyText"
      highlight-current-row
      @current-change="handleCurrentChange"
      @sort-change="sort"
      :default-sort="{prop: 'receivedDate', order: 'descending'}"
      class="table"
      type="selection"
      reserve-selection="true"
      row-key="id"
      :row-class-name="getRowClass"
      ref="table"
      stripe
    >
      <el-table-column
        property="receivedDate"
        label="Received"
        width="180"
        sortable="custom"
        :formatter="formatDate"
      ></el-table-column>
      <el-table-column property="from" label="From" width="120" sortable="custom"></el-table-column>
      <el-table-column property="to" label="To" width="120" sortable="custom"></el-table-column>
      <el-table-column property="subject" label="Subject" sortable="custom">
        <template slot-scope="scope">
          {{scope.row.subject}}
          <i
            class="el-icon-paperclip"
            v-if="scope.row.attachmentCount"
            :title="scope.row.attachmentCount + ' attachments'"
          ></i>
        </template>
      </el-table-column>
    </el-table>
  </div>
</template>
<script lang="ts">
import { Component, Watch } from "vue-property-decorator";
import Vue from "vue";
import { DefaultSortOptions, ElTable } from "element-ui/types/table";
import MessagesController from "../ApiClient/MessagesController";
import MessageSummary from "../ApiClient/MessageSummary";
import * as moment from 'moment';
import HubConnectionManager from "../HubConnectionManager";
import sortedArraySync from "../sortedArraySync";
import { Mutex } from "async-mutex";
import MessageNotificationManager from "../MessageNotificationManager";
import { debounce } from "ts-debounce";
import localeindexof from "locale-index-of";
import HubConnectionStatus from "@/components/hubconnectionstatus.vue";
import ConfirmationDialog from "@/components/confirmationdialog.vue";

@Component({
  components: {
    hubconnstatus: HubConnectionStatus,
    confirmationdialog: ConfirmationDialog
  }
})
export default class MessageList extends Vue {
  constructor() {
    super();

    this.connection = new HubConnectionManager("hubs/messages", this.refresh);
    this.connection.on("messageschanged", async () => {
      await this.refresh();
    });
    this.connection.start();
  }

  private selectedSortDescending: boolean = true;
  private selectedSortColumn: string = "receivedDate";
  private emptyText = "No messages";

  connection: HubConnectionManager;
  messages: MessageSummary[] = [];
  filteredMessages: MessageSummary[] = [];

  error: Error | null = null;
  selectedmessage: MessageSummary | null = null;
  searchTerm: string = "";
  loading = false;
  private messageNotificationManager = new MessageNotificationManager(
    message => {
      this.selectMessage(message);
      this.handleCurrentChange(message);
    }
  );

  selectMessage(message: MessageSummary) {
    (<ElTable>this.$refs.table).setCurrentRow(message);
    this.handleCurrentChange(message);
  }

  handleCurrentChange(message: MessageSummary | null) {
    this.selectedmessage = message;
    this.$emit("selected-message-changed", message);
  }

  formatDate(
    row: number,
    column: number,
    cellValue: Date
  ): string {
    return (<any> moment)(cellValue).format("YYYY-MM-DD HH:mm:ss");
  }

  getRowClass(event: { row: MessageSummary }): string {
    return event.row.isUnread ? "unread" : "read";
  }

  async deleteSelected() {
    if (this.selectedmessage == null) {
      return;
    }

    let messageToDelete = this.selectedmessage;

    let nextIndex = this.filteredMessages.indexOf(messageToDelete)+1;
    if (nextIndex < this.filteredMessages.length){
      this.selectMessage(this.filteredMessages[nextIndex]);
    }

    try {
      await new MessagesController().delete(messageToDelete.id);
      await this.refresh();
    } catch (e) {
      this.error = e;
    }
  }

  async clear() {
    try {
      await new MessagesController().deleteAll();
      await this.refresh();
    } catch (e) {
      this.error = e;
    }
  }

  @Watch("searchTerm")
  doSearch() {
    this.debouncedUpdateFilteredMessages();
  }

  debouncedUpdateFilteredMessages = debounce(this.updateFilteredMessages, 200);

  updateFilteredMessages() {
    if (this.searchTerm) {
      this.emptyText = "No messages matching '" + this.searchTerm + "'";
    } else {
      this.emptyText = "No messages";
    }

    sortedArraySync(
      this.messages.filter(
        m =>
          !this.searchTerm ||
          m.subject.localeIndexOf(this.searchTerm, undefined, {
            sensitivity: "base"
          }) != -1 ||
          m.to.localeIndexOf(this.searchTerm, undefined, {
            sensitivity: "base"
          }) != -1 ||
          m.from.localeIndexOf(this.searchTerm, undefined, {
            sensitivity: "base"
          }) != -1
      ),
      this.filteredMessages,
      (a: MessageSummary, b: MessageSummary) => a.id == b.id,
      (sourceItem: MessageSummary, targetItem: MessageSummary) => {
        targetItem.isUnread = sourceItem.isUnread;
      }
    );

    if (!this.filteredMessages.some(m => this.selectedmessage != null && m.id == this.selectedmessage.id)) {
      this.handleCurrentChange(null);
    }
  }

  private lastSort: string | null = null;
  private lastSortDescending: boolean = false;
  private mutex = new Mutex();
  private initialLoadDone = false;

  refresh = async () => {
    var unlock = await this.mutex.acquire();

    try {
      this.error = null;
      this.loading = true;

      //Copy in case they are mutated during the async load below
      let sortColumn = this.selectedSortColumn;
      let sortDescending = this.selectedSortDescending;

      let serverMessages = await new MessagesController().getSummaries(
        sortColumn,
        sortDescending
      );

      if (
        !this.lastSort ||
        this.lastSort != sortColumn ||
        this.lastSortDescending != sortDescending ||
        serverMessages.length == 0
      ) {
        this.messages.splice(0, this.messages.length, ...serverMessages);
      } else {
        sortedArraySync(
          serverMessages,
          this.messages,
          (a: MessageSummary, b: MessageSummary) => a.id == b.id,
          (sourceItem: MessageSummary, targetItem: MessageSummary) => {
            targetItem.isUnread = sourceItem.isUnread;
          }
        );
      }

      if (this.initialLoadDone) {
        this.messageNotificationManager.notifyMessages(this.messages);
      } else {
        this.messageNotificationManager.setInitialMessages(this.messages);
      }

      this.updateFilteredMessages();

      this.initialLoadDone = true;
      this.lastSort = sortColumn;
      this.lastSortDescending = this.selectedSortDescending;
    } catch (e) {
      console.error(e);
      this.error = e;
    } finally {
      this.loading = false;
      unlock();
    }
  };

  sort = async (sortOptions: DefaultSortOptions) => {
    let descending: boolean = true;
    if (sortOptions.order === "ascending") {
      descending = false;
    }

    if (
      this.selectedSortColumn != sortOptions.prop ||
      this.selectedSortDescending != descending
    ) {
      this.selectedSortColumn = sortOptions.prop || "receivedDate";
      this.selectedSortDescending = descending;

      await this.refresh();
    }
  };

  async created() {
    await this.refresh();
  }

  async destroyed() {
    await this.connection.stop();
  }
}
</script>