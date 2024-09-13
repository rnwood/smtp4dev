<template>
    <div class="messageview fill vfillpanel" v-loading.body="loading">
        <el-alert v-if="error" type="error">
            {{error.message}}
            <el-button v-on:click="loadMessage">Retry</el-button>
        </el-alert>

        <el-dialog v-model="replyDialogVisible" :title="replyAll ? 'Reply All' : 'Reply'" destroy-on-close append-to-body align-center width="80%">
            <messagecompose :reply-to-message="message" @closed="() => replyDialogVisible=false" :replyAll="replyAll" />
        </el-dialog>


        <div v-if="messageSummary" class="hfillpanel fill nogap">
            <div class="toolbar">
                <el-dropdown split-button @command="reply" @click="reply" :disabled="!message || !isRelayAvailable" icon="Reply">
                    Reply
                    <template #dropdown>
                        <el-dropdown-menu>
                            <el-dropdown-item icon="arrow-right" command="reply" title="Reply">Reply</el-dropdown-item>
                            <el-dropdown-item icon="arrow-right" command="replyAll" title="Reply All">Reply All</el-dropdown-item>
                        </el-dropdown-menu>
                    </template>
                </el-dropdown>
                <el-button-group>



                    <el-button v-on:click="relay" icon="d-arrow-right" :disabled="!messageSummary || !isRelayAvailable" :loading="isRelayInProgress" title="Relay...">Relay...</el-button>
                    <el-button icon="download"
                               v-on:click="download">Download</el-button>


                </el-button-group>
            </div>

            <div class="pad">
                <el-alert v-for="warning in warnings"
                          v-bind:key="warning.details"
                          :title="'Warning: ' + warning.details"
                          type="warning"
                          show-icon />

                <div class="messageviewheader" v-if="message">
                    <p :class="{expanded: fromExpanded}" :title="message.from" @click="fromExpanded = !fromExpanded">

                        From: <span v-if="message">{{message.from}}</span>
                    </p>


                    <p :class="{expanded: toExpanded}" :title="message.to?.join(', ')" @click="toExpanded = !toExpanded">
                        To:
                        <span v-for="recip in message.to" :key="recip">
                            <strong v-if="message.deliveredTo.includes(recip)">{{recip}}</strong>
                            <span v-if="!message.deliveredTo.includes(recip)">{{recip}}</span>
                            <span>, </span>
                        </span>
                    </p>

                    <p v-if="message.cc.length" :class="{expanded: ccExpanded}" :title="message.cc?.join(', ')" @click="ccExpanded = !ccExpanded">
                        To:
                        <span v-for="recip in message.cc" :key="recip">
                            <strong v-if="message.deliveredTo.includes(recip)">{{recip}}</strong>
                            <span v-if="!message.deliveredTo.includes(recip)">{{recip}}</span>
                            <span>, </span>
                        </span>
                    </p>


                    <p v-if="message.bcc.length" :class="{expanded: bccExpanded}" :title="message.bcc?.join(', ')" @click="bccExpanded = !bccExpanded">
                        To:
                        <span v-for="recip in message.bcc" :key="recip">
                            <strong v-if="message.deliveredTo.includes(recip)">{{recip}}</strong>
                            <span v-if="!message.deliveredTo.includes(recip)">{{recip}}</span>
                            <span>, </span>
                        </span>
                    </p>

                    <p :class="{expanded: subjectExpanded}" :title="message.subject" @click="subjectExpanded = !subjectExpanded">

                        Subject: <span v-if="message">{{message.subject}}</span>
                    </p>


                </div>


                <div v-if="message && message.relayError">
                    <el-alert type="error">Message relay error: {{message.relayError}}</el-alert>
                </div>

                <template v-if="message && message.mimeParseError">
                    <el-alert type="error">Message parse error: {{message.mimeParseError}}</el-alert>
                </template>
            </div>

            <el-tabs v:model="selectedTabId" style="height: 100%; width:100%" class="fill" type="border-card">
                <el-tab-pane id="view" class="hfillpanel">
                    <template #label>
                        <el-icon><View /></el-icon>&nbsp;View
                    </template>
                    <messageviewattachments :message="message" v-if="message && messageSummary.attachmentCount"></messageviewattachments>

                    <messageview-html v-if="message && message.hasHtmlBody && !message.hasPlainTextBody" :message="message" class="fill messagepreview"></messageview-html>
                    <messageview-plaintext v-if="message && !message.hasHtmlBody && message.hasPlainTextBody" :message="message" class="fill messageplaintext"></messageview-plaintext>
                    <div v-if="message && !message.hasHtmlBody && !message.hasPlainTextBody">This MIME message has no HTML or plain text body.</div>

                    <el-tabs v-if="message && message.hasPlainTextBody && message.hasHtmlBody" value="html" style="height: 100%; width:100%" class="fill">
                        <el-tab-pane id="html" label="HTML" class="hfillpanel">
                            <messageview-html :message="message" class="fill messagepreview"></messageview-html>
                        </el-tab-pane>
                        <el-tab-pane id="plaintext" label="Plain text" class="hfillpanel">
                            <messageview-plaintext :message="message" class="fill messageplaintext"></messageview-plaintext>
                        </el-tab-pane>
                    </el-tabs>
                </el-tab-pane>

                <el-tab-pane label="Analysis" id="analysis">
                    <template #label>

                        <el-icon><FirstAidKit /></el-icon>&nbsp;Analysis
                        <el-tag v-if="totalAnalysisWarningCount" style="margin-left: 6px;" type="warning" size="small" effect="dark" round><el-icon><WarnTriangleFilled /></el-icon> {{totalAnalysisWarningCount ? totalAnalysisWarningCount : ''}}</el-tag>

                    </template>

                    <el-tabs value="clients">
                        <el-tab-pane label="HTML Compatibility" id="clients" class="hfillpanel">
                            <template #label>
                                HTML Compatibility
                                <el-tag v-if="analysisWarningCount.clients" style="margin-left: 6px;" type="warning" size="small" effect="dark" round><el-icon><WarnTriangleFilled /></el-icon> {{analysisWarningCount.clients ? analysisWarningCount.clients : ''}}</el-tag>

                            </template>
                            <messageclientanalysis class="fill" :message="message" @warning-count-changed="n => this.analysisWarningCount.clients=n"></messageclientanalysis>
                        </el-tab-pane>

                        <el-tab-pane label="HTML Validation" id="html" class="hfillpanel">
                            <template #label>
                                HTML Validation
                                <el-tag v-if="analysisWarningCount.html" style="margin-left: 6px;" type="warning" size="small" effect="dark" round><el-icon><WarnTriangleFilled /></el-icon> {{analysisWarningCount.html ? analysisWarningCount.html : ''}}</el-tag>
                            </template>
                            <messagehtmlvalidation class="fill" :message="message" @warning-count-changed="n => this.analysisWarningCount.html=n"></messagehtmlvalidation>
                        </el-tab-pane>
                    </el-tabs>
                </el-tab-pane>

                <el-tab-pane label="Source" id="source" class="fill vfillpanel">
                    <template #label>
                        <el-icon><Document /></el-icon>&nbsp;Source
                    </template>
                    <messagesource class="fill" :message="message" type="source"></messagesource>
                </el-tab-pane>

                <el-tab-pane name="headers" class="hfillpanel">
                    <template #label>
                        <el-icon><Memo /></el-icon>&nbsp;Headers
                    </template>
                    <headers :headers="headers" class="fill"></headers>
                </el-tab-pane>

                <el-tab-pane name="parts" class="hfillpanel">
                    <template #label>
                        <el-icon><document-copy /></el-icon>&nbsp;Parts
                    </template>

                    <el-tree v-if="message"
                             :data="message.parts"
                             :props="{label: 'name', children: 'childParts', disabled: false, isLeaf: isLeaf}"
                             @node-click="onPartSelection"
                             highlight-current
                             empty-message="No parts"
                             accordion
                             node-key="id"
                             :current-node-key="'0'"
                             :default-expanded-keys="['0']">
                        <template v-slot="{node, data}">
                            <span class="custom-tree-node">
                                <i :class="{'document-copy': data.childParts.length, 'document': data.childParts.length == 0 && !data.isAttachment, 'paperclip': data.childParts.length == 0 && data.isAttachment}">
                                </i>
                                {{ node.label }}
                            </span>
                        </template>
                    </el-tree>

                    <div v-show="selectedPart" class="fill vfillpanel">
                        <el-tabs value="headers" class="fill hfillpanel" type="border-card">
                            <el-tab-pane label="Headers" id="headers" class="fill vfillpanel">
                                <template #label>
                                    <el-icon><Memo /></el-icon>&nbsp;Headers
                                </template>
                                <headers :headers="selectedPartHeaders" class="fill"></headers>
                            </el-tab-pane>

                            <el-tab-pane label="Source" id="source" class="fill vfillpanel">
                                <template #label>
                                    <el-icon><Document /></el-icon>&nbsp;Source
                                </template>
                                <messagepartsource class="fill" :messageEntitySummary="selectedPart" type="source"></messagepartsource>
                            </el-tab-pane>

                            <el-tab-pane label="Source (Encoded)" id="raw" class="fill vfillpanel">
                                <template #label>
                                    <el-icon><Document /></el-icon>&nbsp;Source (Encoded)
                                </template>
                                <messagepartsource class="fill" :messageEntitySummary="selectedPart" type="raw"></messagepartsource>
                            </el-tab-pane>
                        </el-tabs>
                    </div>

                    <div v-if="!selectedPart" class="fill vfillpanel">
                        <div class="fill nodetails centrecontents">
                            <div>No part selected.</div>
                        </div>
                    </div>
                </el-tab-pane>
            </el-tabs>
        </div>

        <div v-if="!messageSummary" class="fill nodetails centrecontents">
            <div>No message selected.</div>
        </div>
    </div>
</template>
<script lang="ts">
    import { Component, Vue, Prop, Watch, toNative, } from "vue-facing-decorator";

    import MessagesController from "../ApiClient/MessagesController";
    import MessageSummary from "../ApiClient/MessageSummary";
    import Message from "../ApiClient/Message";
    import MessageEntitySummary from "../ApiClient/MessageEntitySummary";
    import MessageWarning from "../ApiClient/MessageWarning";
    import Headers from "@/components/headers.vue";
    import MessageViewHtml from "@/components/messageviewhtml.vue";
    import MessageviewAttachments from "@/components/messageviewattachments.vue";
    import MessagePartsSource from "@/components/messagepartsource.vue";
    import MessageSource from "@/components/messagesource.vue";
    import MessageClientAnalysis from "@/components/messageclientanalysis.vue";
    import MessageHtmlValidation from "@/components/messagehtmlvalidation.vue";
    import { ElMessageBox, ElNotification } from 'element-plus';
    import ServerController from '../ApiClient/ServerController';
    import MessageViewPlainText from "./messageviewplaintext.vue";
    import MessageCompose from "@/components/messagecompose.vue";

    @Component({
        components: {
            headers: Headers,
            "messageview-html": MessageViewHtml,
            "messageview-plaintext": MessageViewPlainText,
            messageviewattachments: MessageviewAttachments,
            messagepartsource: MessagePartsSource,
            messagesource: MessageSource,
            messageclientanalysis: MessageClientAnalysis,
            messagehtmlvalidation: MessageHtmlValidation,
            messagecompose: MessageCompose
        }
    })
    class MessageView extends Vue {

        @Prop({})
        messageSummary: MessageSummary | null = null;
        selectedTabId = "view";
        message: Message | null = null;
        selectedPart: MessageEntitySummary | null = null;
        warnings: MessageWarning[] = [];
        analysisWarningCount = { clients: 0, html: 0 };

        get totalAnalysisWarningCount() { return Object.values(this.analysisWarningCount).reduce((a, c) => a + c) }

        error: Error | null = null;
        loading = false;


        isRelayInProgress: boolean = false;
        isRelayAvailable: boolean = false;

        replyDialogVisible = false;
        replyAll = false;

        toExpanded = false;
        fromExpanded = false;
        ccExpanded = false;
        bccExpanded = false;
        subjectExpanded = false;

        @Watch("messageSummary")
        async onMessageSummaryChange(
            value: MessageSummary | null,
            oldValue: MessageSummary | null
        ) {
            await this.loadMessage();
        }

        async loadMessage() {
            this.error = null;
            this.loading = true;
            this.message = null;
            this.selectedPart = null;

            try {
                if (this.messageSummary != null) {
                    this.message = await new MessagesController().getMessage(
                        this.messageSummary.id
                    );
                    this.selectedPart = this.message.parts[0];
                    this.setWarnings();

                    if (this.messageSummary.isUnread) {
                        var currentMessageSummary = this.messageSummary;
                        setTimeout(async () => {
                            if (
                                this.messageSummary != null &&
                                currentMessageSummary.id == this.messageSummary.id
                            ) {
                                try {
                                    await new MessagesController().markMessageRead(
                                        this.messageSummary.id
                                    );
                                } catch (e) {
                                    console.error(e);
                                }
                            }
                        }, 2000);
                    }
                }
            } catch (e: any) {
                this.error = e;
            } finally {
                this.loading = false;
            }
        }

        async setWarnings() {
            var result: MessageWarning[] = [];

            if (this.message != null) {
                var parts = this.message.parts;
                this.getWarnings(parts, result);
            }
            this.warnings = result;
        }

        getWarnings(parts: MessageEntitySummary[], result: MessageWarning[]) {
            for (let part of parts) {
                for (let warning of part.warnings) {
                    result.push(warning);
                }

                this.getWarnings(part.childParts, result);
            }
        }

        isLeaf(value: MessageEntitySummary[] | MessageEntitySummary) {
            return (
                !(value as MessageEntitySummary[]).length &&
                !(value as MessageEntitySummary).childParts.length
            );
        }

        onPartSelection(part: MessageEntitySummary | null) {
            this.selectedPart = part;
        }

        async reply(command: string) {
            this.replyAll = command == "replyAll";
            this.replyDialogVisible = true;
        }

        async relay() {
            if (this.messageSummary == null) {
                return;
            }

            let emails: string[];

            try {

                let dialogResult = await ElMessageBox.prompt('Email address(es) to relay to (separate multiple with ,)', 'Relay Message', {
                    confirmButtonText: 'OK',
                    inputValue: this.messageSummary.to.join(","),
                    cancelButtonText: 'Cancel',
                    inputPattern: /[^, ]+(, *[^, ]+)*/,
                    inputErrorMessage: 'Invalid email addresses'
                }) as MessageBoxInputData;

                emails = dialogResult.value.split(",").map(e => e.trim());
            } catch {
                return;
            }

            try {
                this.isRelayInProgress = true;
                await new MessagesController().relayMessage(this.messageSummary.id, { overrideRecipientAddresses: emails });

                ElNotification.success({ title: "Relay Message Success", message: "Completed OK" });
            } catch (e: any) {
                var message = e.response?.data?.detail ?? e.sessage;

                ElNotification.error({ title: "Relay Message Failed", message: message });
            } finally {
                this.isRelayInProgress = false;
            }
        }

        async download() {
            if (this.messageSummary == null) {
                return;
            }
            window.open(
                new MessagesController().downloadMessage_url(this.messageSummary.id)
            );
        }

        async mounted() {
            await this.loadMessage();
            this.isRelayAvailable = !!await (await new ServerController().getServer()).relaySmtpServer;
        }

        async destroyed() { }

        get headers() {
            return this.message != null ? this.message.headers : [];
        }

        get selectedPartHeaders() {
            return this.selectedPart != null ? this.selectedPart.headers : [];
        }
    }

    export default toNative(MessageView);
</script>