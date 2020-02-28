<template>
    <el-button size="small" :type="buttonType()" :icon="buttonIcon()" @click="buttonClick" :title="buttonTitle()"></el-button>
</template>

<script lang="ts">
    import Vue from "vue";
    import { Component, Prop } from "vue-property-decorator";
    import HubConnectionManager from "../HubConnectionManager";

    @Component
    export default class HubConnectionStatus extends Vue {

        constructor() {
            super();
        }

        @Prop({ default: undefined })
        connection: HubConnectionManager | undefined;

        buttonType(): string {
            return this.connection && this.connection.connected ? "success" : this.connection && this.connection.started ? "warning" : "danger";
        }

        buttonTitle(): string {
            var errorText = this.connection && this.connection.error ? "\nError: " + this.connection.error.message : "";

            return (this.connection && this.connection.connected ? "Auto refresh enabled - Click to disconnect" : this.connection && this.connection.started ? "Auto refresh re-connecting..." : "Auto refresh disabled - click to enable") + errorText;
        }

        buttonIcon(): string {
            return this.connection && this.connection.connected ? "el-icon-success" : this.connection && this.connection.started ? "el-icon-warning" : "el-icon-warning";
        }


        buttonClick = () => {
            if (this.connection) {

                if (!this.connection.started) {
                    this.connection.start();
                } else {
                    this.connection.stop();
                }
            }
        }

        async created() {

        }

        async destroyed() {

        }

    }
</script>