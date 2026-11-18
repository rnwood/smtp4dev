<template>
    <div class="fakegatewayview smsview fill hfillpanel" data-testid="sms-view">
        <div v-if="sms" class="details">
            <div class="headline">
                <span class="headline-to" data-testid="sms-view-to">To: {{ sms.to }}</span>
                <el-tag :type="sms.status === 'ACCEPTED' ? 'success' : 'danger'" data-testid="sms-view-status">{{ sms.status }}</el-tag>
            </div>
            <div v-if="sms.sender">From: {{ sms.sender }}</div>

            <el-alert v-if="sms.status === 'REFUSED'"
                      type="warning"
                      show-icon
                      :closable="false"
                      title="The fake gateway refused this SMS (it returned Status=1): the number is in RefuseNumbers or the request had no SmsId, phone number or text."
                      data-testid="sms-view-refused" />

            <div class="bubble" :class="{ empty: !sms.text }" data-testid="sms-view-text">{{ sms.text || "(no text)" }}</div>

            <div class="toolbar">
                <el-button icon="CopyDocument" :disabled="!sms.text" @click="copyText" data-testid="sms-view-copy">Copy text</el-button>
                <el-button v-if="effectiveCode" icon="CopyDocument" @click="copyCode" data-testid="sms-view-copy-code">Copy code {{ effectiveCode }}</el-button>
                <el-button icon="Delete" @click="deleteSms" data-testid="sms-view-delete">Delete</el-button>
            </div>

            <el-descriptions :column="1" border size="small">
                <el-descriptions-item label="Received">{{ formatTime(sms.receivedAt) }} ({{ sms.receivedAt }})</el-descriptions-item>
                <el-descriptions-item label="SmsId">{{ sms.externalSmsId || "-" }}</el-descriptions-item>
                <el-descriptions-item label="MsgId">{{ sms.msgId }}</el-descriptions-item>
                <el-descriptions-item label="Encoding">{{ sms.encoding }} ({{ sms.segments }} {{ sms.segments === 1 ? "segment" : "segments" }})</el-descriptions-item>
                <el-descriptions-item label="User">{{ sms.gatewayUser || "-" }}</el-descriptions-item>
                <el-descriptions-item label="Send time">{{ formatSendTime(sms.sendTime) }}</el-descriptions-item>
                <el-descriptions-item v-if="sms.contentType" label="Content type">{{ sms.contentType }}</el-descriptions-item>
            </el-descriptions>
        </div>

        <div v-else class="fill nodetails centrecontents">
            <div>No SMS selected</div>
        </div>
    </div>
</template>
<script lang="ts">
    import { Component, Vue, Prop, toNative, Emit } from "vue-facing-decorator";
    import { ElNotification } from "element-plus";
    import FakeGatewaysController, { SmsMessage } from "../../ApiClient/FakeGatewaysController";
    import { copyToClipboard, formatLocalTime } from "./clipboard";

    @Component({})
    class SmsView extends Vue {

        @Prop({ default: null })
        sms: SmsMessage | null = null;

        formatTime(iso: string): string {
            return formatLocalTime(iso);
        }

        // yyyyMMddHHmmss -> yyyy-MM-dd HH:mm:ss, anything else is shown as received
        formatSendTime(value: string | null): string {
            if (!value) {
                return "-";
            }
            const match = /^(\d{4})(\d{2})(\d{2})(\d{2})(\d{2})(\d{2})$/.exec(value);
            return match ? `${match[1]}-${match[2]}-${match[3]} ${match[4]}:${match[5]}:${match[6]}` : value;
        }

        // The API fills code only when the caller asked for a codeRegex, so look for a typical verification code here
        get effectiveCode(): string | null {
            if (!this.sms) {
                return null;
            }
            return this.sms.code || /\b\d{4,8}\b/.exec(this.sms.text ?? "")?.[0] || null;
        }

        copyText() {
            if (this.sms) {
                void copyToClipboard(this.sms.text);
            }
        }

        copyCode() {
            if (this.effectiveCode) {
                void copyToClipboard(this.effectiveCode);
            }
        }

        @Emit("deleted")
        async deleteSms() {
            if (!this.sms) {
                return null;
            }

            const id = this.sms.id;
            try {
                await new FakeGatewaysController().deleteSms(id);
            } catch (e: any) {
                // 404 means it is already gone (e.g. deleted by a test), which is the desired state
                if (e?.response?.status !== 404) {
                    ElNotification.error({ title: "Delete SMS Failed", message: e.message });
                    return null;
                }
            }
            return id;
        }
    }

    export default toNative(SmsView)
</script>
<style>
    .fakegatewayview {
        padding: 0 12px 12px 12px;
        overflow: auto;
    }

    .fakegatewayview .details {
        display: flex;
        flex-direction: column;
        gap: 12px;
    }

    .fakegatewayview .headline {
        display: flex;
        align-items: center;
        gap: 12px;
        font-size: 1.2em;
        font-weight: bold;
    }

    .fakegatewayview .bubble {
        white-space: pre-wrap;
        word-break: break-word;
        padding: 12px;
        border-radius: 8px;
        border: 1px solid var(--el-border-color);
        background-color: var(--el-fill-color-light);
        color: var(--el-text-color-primary);
    }

    .fakegatewayview .bubble.empty {
        color: var(--el-text-color-placeholder);
        font-style: italic;
    }
</style>
