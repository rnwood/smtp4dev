<template>
  <el-row>
    <el-col :span="16">
      <el-pagination
        background
        layout="prev, pager, next"
        :page-size="pageSize"
        :page-count="pageCount"
        :pager-count="5"
        :current-page="page"
        v-on:current-change="onCurrentPageChange"
        v-on:size-change="onPageSizeChange"
        :total="totalItems"
      >
      </el-pagination>
    </el-col>
    <el-col :span="4">
      <span>Page size:</span>
    </el-col>
    <el-col :span="4">
      <el-input
        placeholder="Page size"
        v-model.number="pageSize"
        min="1"
        type="number"
        @change="onPageSizeChange"
        size="small"
      ></el-input>
    </el-col>
  </el-row>
</template>

<script lang="ts">
import { Component, Emit, Prop, Watch } from "vue-property-decorator";
import Vue from "vue";
import PagedResult from "@/ApiClient/PagedResult";
import ClientController from "@/ApiClient/ClientController";


@Component({})
export default class MessageListPager extends Vue {
  constructor() {
    super();
  }

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
    let client = await new ClientController().getClient();
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
</script>
