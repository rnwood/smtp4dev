import { Component, Prop, Watch } from 'vue-property-decorator';
import Vue from 'vue'
import AceEditor from 'vue2-ace-editor';
import { Editor } from 'brace';

@Component({
    components: {
        aceeditor: AceEditor
    }
}
)
export default class TextView extends Vue {
    constructor() {
        super(); 
    }

    @Prop()
    text: string | null = null;

    editorInit(brace: Editor) {
        require('brace/ext/language_tools') //language extension prerequsite...
        require('brace/mode/text')
        require('brace/theme/chrome')
        require('brace/snippets/javascript') //snippet

        brace.setReadOnly(true);
    }

    get textToShow() {
        if (!this.text) {
            return "";
        }

        return this.text;
    }

    set textToShow(value: string) {
        //ignore
    }

    async destroyed() {
        
    }


}