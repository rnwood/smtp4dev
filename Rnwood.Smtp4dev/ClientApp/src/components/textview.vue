<template>

    
        <aceeditor v-model:value="textToShow" @init="editorInit" theme="chrome" lang="text" width="100%" :wrap="true" height="100%"></aceeditor>
    
</template>

<script lang="ts">
    import { Component, Vue, Prop, toNative } from 'vue-facing-decorator';
    
    import { VAceEditor } from 'vue3-ace-editor';
    import { Editor } from 'brace';
    import "ace-builds/src-noconflict/mode-text";
    import "ace-builds/src-noconflict/theme-chrome";

    

    @Component({
        components: {
            aceeditor: VAceEditor
        }
    }
    )
    class TextView extends Vue {
        
        @Prop()
        text: string | null = null;

        editorInit(brace: Editor) {
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

    export default toNative(TextView)
</script>