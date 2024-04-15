<template>
  <div style="display: flex; align-items: stretch; column-gap: 12px;">

    <el-pagination style="flex: 1 1 auto" background layout="prev, pager, next" :page-size="pageSize"
      :page-count="pageCount" :pager-count="5" :current-page="page" v-on:current-change="onCurrentPageChange"
      v-on:size-change="onPageSizeChange" :total="totalItems">
    </el-pagination>
    <div style="flex: 0 0 content; align-self: center;">Page size:</div>


    <el-input style="flex: 0 0 85px;" placeholder="Page size" v-model.number="pageSize" min="1" type="number" @change="onPageSizeChange"
      size="small"></el-input>
  </div>
</template>

<script lang="ts">
import { Component, Vue, Emit, Prop, Watch, toNative } from "vue-facing-decorator";

import PagedResult from "@/ApiClient/PagedResult";
import ClientSettingsController from "@/ApiClient/ClientSettingsController";


@Component({})
class MessageListPager extends Vue {

  page: number = 1;
  pageSize: number = 25;
  pageCount: number = 1;
  totalItems: number = 1;

  @Prop({})
  readonly pagedData: PagedResult<any> | undefined;

  async created() {
    await this.initPageSizeProps();
  }

  @Watch("pagedData")
  async onPagedDataChange(
    value: PagedResult<any> | null,
    oldValue: PagedResult<any> | null
  ) {
    if (value) {
      await this.updatePagination(value);
    }
  }

  @Emit()
  onCurrentPageChange(page: number) {
    this.page = page;
    return page;
  }

  @Emit()
  onPageSizeChange(pageSize: number) {
    if (pageSize < 1) this.pageSize = 1;
    return this.pageSize;
  }

  private async initPageSizeProps() {
    const defaultPageSize = 25;
    let client = await new ClientSettingsController().getClientSettings();
    this.pageSize = client.pageSize || defaultPageSize;
  }

  updatePagination<Type>(pagedData: PagedResult<Type>): void {
    this.pageCount = pagedData.pageCount;
    this.totalItems = pagedData.rowCount;

    // reset to last page if we're beyond the last page.
    if (this.pageCount < pagedData.currentPage) {
      this.page = this.pageCount;
    }
  }
    }
    export default toNative(MessageListPager)
</script>
