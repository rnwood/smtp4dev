<template>
    <div class="fakegatewayview targitoview fill hfillpanel" data-testid="targito-view">
        <div v-if="item" class="details">
            <div class="headline">
                <span data-testid="targito-view-method">{{ item.method }}</span>
            </div>

            <div class="toolbar">
                <el-button icon="Delete" @click="deleteItem" data-testid="targito-view-delete">Delete</el-button>
            </div>

            <el-descriptions :column="1" border size="small">
                <el-descriptions-item v-if="item.oldEmail" label="Old e-mail">{{ item.oldEmail }}</el-descriptions-item>
                <el-descriptions-item :label="item.oldEmail ? 'New e-mail' : 'To'">{{ item.email || "-" }}</el-descriptions-item>
                <el-descriptions-item v-if="item.templateId || item.templateName" label="Template">
                    {{ item.templateId || "-" }}<span v-if="item.templateName"> ({{ item.templateName }})</span>
                </el-descriptions-item>
                <el-descriptions-item v-if="item.senderDomain" label="Sender domain">{{ item.senderDomain }}</el-descriptions-item>
                <el-descriptions-item v-if="item.sendDateTime" label="Send date/time">{{ item.sendDateTime }}</el-descriptions-item>
                <el-descriptions-item label="Result">{{ item.resultId || "-" }}</el-descriptions-item>
                <el-descriptions-item label="User">{{ item.accountUser || "-" }}</el-descriptions-item>
                <el-descriptions-item label="Received">{{ formatTime(item.receivedAt) }} ({{ item.receivedAt }})</el-descriptions-item>
            </el-descriptions>

            <div v-if="dataRows.length > 0">
                <h4>Data</h4>
                <table class="datatable" data-testid="targito-view-data">
                    <tr v-for="row in dataRows" :key="row.key" :data-key="row.key">
                        <td class="datakey">{{ row.key }}</td>
                        <td class="datavalue">
                            <a v-if="linkFor(row.value)"
                               :href="linkFor(row.value)!"
                               target="_blank"
                               rel="noopener noreferrer"
                               class="datalink">{{ row.value }}</a>
                            <span v-else class="datatext">{{ row.value }}</span>
                        </td>
                        <td class="datacopy">
                            <el-button size="small" icon="CopyDocument" title="Copy value" :aria-label="'Copy value of ' + row.key" @click="copy(row.value)"></el-button>
                        </td>
                    </tr>
                </table>
            </div>

            <div>
                <h4>Raw params (JSON)</h4>
                <pre class="rawparams" data-testid="targito-view-raw">{{ rawParams }}</pre>
            </div>
        </div>

        <div v-else class="fill nodetails centrecontents">
            <div>No Targito call selected</div>
        </div>
    </div>
</template>
<script lang="ts">
    import { Component, Vue, Prop, toNative, Emit } from "vue-facing-decorator";
    import { ElNotification } from "element-plus";
    import FakeGatewaysController, { TargitoRecord } from "../../ApiClient/FakeGatewaysController";
    import { copyToClipboard, formatLocalTime } from "./clipboard";
    import { safeHttpUrl } from "./safeHttpUrl";

    @Component({})
    class TargitoView extends Vue {

        @Prop({ default: null })
        item: TargitoRecord | null = null;

        get dataRows(): { key: string, value: string }[] {
            if (!this.item || !this.item.data) {
                return [];
            }
            return Object.entries(this.item.data).map(([key, value]) => ({ key, value: value ?? "" }));
        }

        get rawParams(): string {
            return this.item ? JSON.stringify(this.item.params, null, 2) : "";
        }

        linkFor(value: string): string | null {
            return safeHttpUrl(value);
        }

        formatTime(iso: string): string {
            return formatLocalTime(iso);
        }

        copy(value: string) {
            copyToClipboard(value);
        }

        @Emit("deleted")
        async deleteItem() {
            if (!this.item) {
                return null;
            }

            const id = this.item.id;
            try {
                await new FakeGatewaysController().deleteTargito(id);
            } catch (e: any) {
                // 404 means it is already gone, which is the desired state
                if (e?.response?.status !== 404) {
                    ElNotification.error({ title: "Delete Targito Failed", message: e.message });
                    return null;
                }
            }
            return id;
        }
    }

    export default toNative(TargitoView)
</script>
<style>
    .targitoview .datatable {
        border-collapse: collapse;
        width: 100%;
    }

    .targitoview .datatable td {
        padding: 4px 8px;
        border-bottom: 1px solid var(--el-border-color-lighter);
        vertical-align: top;
        word-break: break-all;
    }

    .targitoview .datakey {
        font-weight: bold;
        white-space: nowrap;
        word-break: normal;
    }

    .targitoview .datacopy {
        width: 1%;
    }

    .targitoview .datalink {
        color: var(--el-color-primary);
    }

    .targitoview .rawparams {
        white-space: pre-wrap;
        word-break: break-all;
        margin: 0;
        padding: 12px;
        border-radius: 4px;
        border: 1px solid var(--el-border-color);
        background-color: var(--el-fill-color-light);
        color: var(--el-text-color-primary);
    }
</style>
