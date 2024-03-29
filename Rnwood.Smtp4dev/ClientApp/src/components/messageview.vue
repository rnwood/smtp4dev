﻿<template>
    <div class="messageview fill vfillpanel" v-loading.body="loading">
        <el-alert v-if="error" type="error">
            {{error.message}}
            <el-button v-on:click="loadMessage">Retry</el-button>
        </el-alert>



        <div v-if="messageSummary" class="hfillpanel fill">
            <div class="toolbar">
                <el-button icon="el-icon-download"
                           v-on:click="download">Download</el-button>

                <el-button v-on:click="relay" icon="el-icon-d-arrow-right" :disabled="!messageSummary || !isRelayAvailable" :loading="isRelayInProgress" title="Relay">Relay</el-button>
            </div>

            <div class="pad">
                <el-alert v-for="warning in warnings"
                          v-bind:key="warning.details"
                          :title="'Warning: ' + warning.details"
                          type="warning"
                          show-icon />

                <table class="messageviewheader">
                    <tr>
                        <td>From:</td>
                        <td><span v-if="message">{{message.from}}</span></td>
                    </tr>

                    <tr>
                        <td>To:</td>
                        <td><span v-if="message">{{message.to}}</span></td>
                    </tr>
                    <tr v-if="message && message.cc">
                        <td>Cc:</td>
                        <td>{{message.cc}}</td>
                    </tr>
                    <tr v-if="message && message.bcc">
                        <td>Bcc:</td>
                        <td>{{message.bcc}}</td>
                    </tr>
                    <tr>
                        <td>Subject:</td>
                        <td><span v-if="message">{{message.subject}}</span></td>
                    </tr>
                    <tr>
                        <td>Secure:</td>
                        <td><span v-if="message">{{message.secureConnection}}</span></td>
                    </tr>
                </table>


                <div v-if="message && message.relayError">
                    <el-alert type="error">Message relay error: {{message.relayError}}</el-alert>
                </div>

                <template v-if="message && message.mimeParseError">
                    <el-alert type="error">Message parse error: {{message.mimeParseError}}</el-alert>
                </template>
            </div>

            <el-tabs value="view" style="height: 100%; width:100%" class="fill" type="border-card">
                <el-tab-pane name="view" class="hfillpanel">
                    <span slot="label">
                        <i class="el-icon-view"></i> View
                    </span>
          <messageviewattachments  :message="message" v-if="message && messageSummary.attachmentCount"></messageviewattachments>

          <messageview-html v-if="message && message.hasHtmlBody && !message.hasPlainTextBody" :message="message" class="fill messagepreview"></messageview-html>
          <messageview-plaintext v-if="message && !message.hasHtmlBody && message.hasPlainTextBody" :message="message" class="fill messageplaintext"></messageview-plaintext>
          <div v-if="message && !message.hasHtmlBody && !message.hasPlainTextBody">This MIME message has no HTML or plain text body.</div>
          
          <el-tabs v-if="message && message.hasPlainTextBody && message.hasPlainTextBody" value="html" style="height: 100%; width:100%" class="fill">
            <el-tab-pane name="html" label="HTML" class="hfillpanel" v-if="message && message.hasHtmlBody">
              <messageview-html :message="message" class="fill messagepreview"></messageview-html>
            </el-tab-pane>
            <el-tab-pane name="plaintext" label="Plain text" class="hfillpanel" v-if="message && message.hasPlainTextBody">
              <messageview-plaintext :message="message" class="fill messageplaintext"></messageview-plaintext>
            </el-tab-pane>
          </el-tabs>
        </el-tab-pane>

        <el-tab-pane label="Source" name="source" class="fill vfillpanel">
          <messagesource class="fill" :message="message" type="source"></messagesource>
        </el-tab-pane>

                <el-tab-pane name="headers" class="hfillpanel">
                    <span slot="label">
                        <i class="el-icon-notebook-2"></i> Headers
                    </span>
                    <headers :headers="headers" class="fill"></headers>
                </el-tab-pane>

                <el-tab-pane name="parts" class="hfillpanel">
                    <span slot="label">
                        <i class="el-icon-document-copy"></i> Parts
                    </span>

                    <el-tree v-if="message"
                             :data="message.parts"
                             :props="{label: 'name', children: 'childParts', disabled: false, isLeaf: isLeaf}"
                             @node-click="onPartSelection"
                             highlight-current
                             empty-message="No parts"
                             ref="partstree"
                             accordion
                             node-key="id"
                             :default-expanded-keys="['0']">
                        <template v-slot="{node, data}">
                            <span class="custom-tree-node">
                                <i :class="{'el-icon-document-copy': data.childParts.length, 'el-icon-document': data.childParts.length == 0 && !data.isAttachment, 'el-icon-paperclip': data.childParts.length == 0 && data.isAttachment}">
                                </i>
                                {{ node.label }}
                            </span>
                        </template>
                    </el-tree>

                    <div v-show="selectedPart" class="fill vfillpanel">
                        <el-tabs value="headers" class="fill hfillpanel" type="border-card">
                            <el-tab-pane label="Headers" name="headers" class="fill vfillpanel">
                                <span slot="label">
                                    <i class="el-icon-notebook-2"></i> Headers
                                </span>
                                <headers :headers="selectedPartHeaders" class="fill"></headers>
                            </el-tab-pane>

                            <el-tab-pane label="Source" name="source" class="fill vfillpanel">
                                <messagepartsource class="fill" :messageEntitySummary="selectedPart" type="source"></messagepartsource>
                            </el-tab-pane>

                            <el-tab-pane label="Raw" name="raw" class="fill vfillpanel">
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
import {Component, Prop, Watch} from "vue-property-decorator";
import Vue from "vue";
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
import {Tree} from "element-ui";
import {MessageBoxInputData} from 'element-ui/types/message-box';
import ServerController from '../ApiClient/ServerController';
import MessageViewPlainText from "./messageviewplaintext.vue";

@Component({
  components: {
    headers: Headers,
    "messageview-html": MessageViewHtml,
    "messageview-plaintext": MessageViewPlainText,
    messageviewattachments: MessageviewAttachments,
    messagepartsource: MessagePartsSource,
    messagesource: MessageSource
  }
})
export default class MessageView extends Vue {
  constructor() {
    super();
  }

        @Prop({})
        messageSummary: MessageSummary | null = null;
        message: Message | null = null;
        selectedPart: MessageEntitySummary | null = null;
        warnings: MessageWarning[] = [];

        error: Error | null = null;
        loading = false;


        isRelayInProgress: boolean = false;
        isRelayAvailable: boolean = false;

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
                    if (this.$refs.partstree) {
                        (<Tree>this.$refs.partstree).setCurrentNode(
                            (<Tree>this.$refs.partstree).getNode(0)
                        );
                    }
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

        async relay() {
            if (this.messageSummary == null) {
                return;
            }

            let emails: string[];

            try {

                let dialogResult = <MessageBoxInputData>await this.$prompt('Email address(es) to relay to (separate multiple with ,)', 'Relay Message', {
                    confirmButtonText: 'OK',
                    inputValue: this.messageSummary.to,
                    cancelButtonText: 'Cancel',
                    inputPattern: /[^, ]+(, *[^, ]+)*/,
                    inputErrorMessage: 'Invalid email addresses'
                });

                emails = (<string>dialogResult.value).split(",").map(e => e.trim());
            } catch {
                return;
            }

            try {
                this.isRelayInProgress = true;
                await new MessagesController().relayMessage(this.messageSummary.id, { overrideRecipientAddresses: emails });

                this.$notify.success({ title: "Relay Message Success", message: "Completed OK" });
            } catch (e: any) {
                var message = e.response?.data?.detail ?? e.sessage;

                this.$notify.error({ title: "Relay Message Failed", message: message });
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
            this.isRelayAvailable = !!await (await new ServerController().getServer()).relayOptions.smtpServer;
        }

        async destroyed() { }

        get headers() {
            return this.message != null ? this.message.headers : [];
        }

        get selectedPartHeaders() {
            return this.selectedPart != null ? this.selectedPart.headers : [];
        }
    }
</script>