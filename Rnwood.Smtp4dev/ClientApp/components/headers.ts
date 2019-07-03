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