import { Component, Prop, Watch } from 'vue-property-decorator';
import Vue, { VNode } from 'vue'

@Component
export default class ConfirmationDialog extends Vue {
    constructor() {
        super();
    }

    mounted() {
        if (this.$slots.default[0].componentInstance) {
            this.$slots.default[0].componentInstance.$on("click", this.getConfirmation);

            //Get rid of the intermediate <span> which breaks styling;
            if (this.$el.parentElement) {
                var parent = this.$el.parentElement;
                parent.insertBefore(this.$slots.default[0].componentInstance.$el, this.$el)
                parent.removeChild(this.$el);
            }
        }
    }

    @Prop({default: ""})
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

            if (this.$slots.default[0].componentInstance) {
                if (!this.$slots.default[0].componentInstance.$el.offsetParent) {
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