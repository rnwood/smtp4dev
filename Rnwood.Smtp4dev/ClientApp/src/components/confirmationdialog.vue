<template>

    <div>
        <el-popover placement="bottom"
                    width="160"
                    v-model="visible"
                    trigger="manual">
            <p>{{message}}</p>
            <div style="text-align: right; margin: 0">
                <el-button size="mini" type="text" @click="cancel()">cancel</el-button>
                <el-button size="mini" type="text" v-if="alwaysIsAvailable()" @click="confirm(true)">always</el-button>
                <el-button type="primary" size="mini" @click="confirm(false)">confirm</el-button>
            </div>
            <slot slot="reference"></slot>
        </el-popover>

    </div>

</template>

<script lang="ts">
    import { Component, Prop } from 'vue-property-decorator';
    import Vue from 'vue'

    @Component
    export default class ConfirmationDialog extends Vue {
        constructor() {
            super();
        }

        mounted() {
            if (this.$slots.default && this.$slots.default.length > 0 && this.$slots.default[0].componentInstance) {
                this.$slots.default[0].componentInstance.$on("click", this.getConfirmation);

                //Get rid of the intermediate <span> which breaks styling;
                if (this.$el.parentElement) {
                    var parent = this.$el.parentElement;
                    parent.insertBefore(this.$slots.default[0].componentInstance.$el, this.$el)
                    parent.removeChild(this.$el);
                }
            }
        }

        @Prop({ default: "" })
        message!: string;

        @Prop({ default: undefined })
        alwaysKey: string | undefined;

        visible: boolean = false;


        alwaysIsAvailable() {
            return this.alwaysKey && window.localStorage;
        }

        cancel() {
            this.visible = false;
        }

        confirm(always: boolean) {

            if (always) {
                window.localStorage.setItem("always-" + this.alwaysKey, "true");
            }

            this.visible = false;
            this.$emit("confirm");
        }

        private checkReferenceVisible() {
            if (this.visible) {

                if (this.$slots.default && this.$slots.default.length > 0 && this.$slots.default[0].componentInstance) {
                    if ((<any>!this.$slots.default[0].componentInstance.$el).offsetParent) {
                        this.visible = false;
                    }
                }

                setTimeout(this.checkReferenceVisible, 100);
            }
        }

        private getConfirmation() {


            if (this.alwaysIsAvailable() && window.localStorage.getItem("always-" + this.alwaysKey) === "true") {
                this.confirm(false);
            } else {
                this.visible = true;
                setTimeout(this.checkReferenceVisible, 100);
            }
        }



    }
</script>