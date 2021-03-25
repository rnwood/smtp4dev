<template>
    <div class="hfillpanel" v-loading="loading">
        <el-alert v-if="error" type="error">
            {{error.message}}

            <el-button v-on:click="loadMessage">Retry</el-button>
        </el-alert>

        <div class="toolbar"><el-button size="small" icon="el-icon-document" @click="download">Open</el-button></div>
        <div v-show="source" class="vfillpanel fill">
            <textview :text="source" class="fill"></textview>
        </div>
    </div>
</template>
<script lang="ts">
    import { Component, Prop, Watch } from 'vue-property-decorator'
    import Vue from 'vue'
    import MessagesController from "../ApiClient/MessagesController";
    import MessageEntitySummary from "../ApiClient/MessageEntitySummary";
    import TextView from "@/components/textview.vue";

    @Component({
        components: {
            textview: TextView
        }
    })
export default class MessagePartSource extends Vue {
                    constructor() {
                    super();
            }

            @Prop()
            messageEntitySummary: MessageEntitySummary | null = null;

            source: string | null = null;
            sourceurl: string | null = null;
            error: Error | null = null;
            loading = false;

    @Prop({ default: "source" } )
                type!: "source" | "raw";

                @Watch("messageEntitySummary")
    async onMessageEntitySummaryChanged(value: MessageEntitySummary, oldValue: MessageEntitySummary) {

                    await this.loadMessage();

            }

    download() {
        if (this.sourceurl) {
                    window.open(this.sourceurl);
            }
        }

    async loadMessage() {

                    this.error = null;
                this.loading = true;
                this.source = null;
                this.sourceurl = null;

        try {
            if (this.messageEntitySummary != null) {
                if (this.type === "raw") {
                    this.sourceurl = new MessagesController().getPartSourceRaw_url(this.messageEntitySummary.messageId, this.messageEntitySummary.id);
                this.source = await new MessagesController().getPartSourceRaw(this.messageEntitySummary.messageId, this.messageEntitySummary.id);

                } else {
                    this.sourceurl = new MessagesController().getPartSource_url(this.messageEntitySummary.messageId, this.messageEntitySummary.id);
                this.source = await new MessagesController().getPartSource(this.messageEntitySummary.messageId, this.messageEntitySummary.id);

            }
        }
        } catch (e) {
                    this.error = e;
        } finally {
                    this.loading = false;
            }
        }

    async created() {
                    this.loadMessage();
            }

    async destroyed() {

                }
                }
</script>