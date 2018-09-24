import { Component, Prop } from 'vue-property-decorator';
import Vue from 'vue';
import Header from "../ApiClient/Header";

@Component({
    template: require('./headers.html')
})
export default class Headers extends Vue {
    constructor() {
        super(); 
    }

    @Prop({ default: null })
    headers: Header[] | null = null;
    

    async created() {

     
    }

    async destroyed() {
        
    }
}