<template>

    <div v-show="headers" class="headers">
        <el-table :data="headers" empty-text="No headers" :default-sort="{prop: 'name', order: 'ascending'}" class="table fill" stripe>
            <el-table-column property="name" label="Name" width="150" sortable>
            </el-table-column>
            <el-table-column property="value" label="Value" sortable>
            </el-table-column>
        </el-table>
    </div>
</template>

<script lang="ts">
    import { Component, Prop, Watch } from 'vue-property-decorator';
import Vue from 'vue'
import Header from "../ApiClient/Header";

@Component
export default class Headers extends Vue {
    constructor() {
        super(); 
    }

    @Prop({ default: [] })
    headers!: Header[];

    async destroyed() {
        
    }


}
</script>