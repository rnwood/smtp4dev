<template>
    <div class="expression-input-container">
        <el-input 
            :model-value="modelValue" 
            @update:model-value="$emit('update:modelValue', $event)"
            :placeholder="placeholder"
            :disabled="disabled">
            <template #prefix>
                <slot name="prefix"></slot>
            </template>
            <template #append>
                <el-button 
                    icon="edit" 
                    @click="openEditor" 
                    :disabled="disabled"
                    title="Open JavaScript Expression Editor">
                    Edit
                </el-button>
            </template>
        </el-input>
        
        <jsexpressioneditor
            v-model:visible="showEditor"
            :value="modelValue"
            :expression-type="expressionType"
            @update:value="onEditorSave"
            @close="showEditor = false" />
    </div>
</template>

<script lang="ts">
import { Component, Vue, Prop, Emit, toNative } from 'vue-facing-decorator';
import JSExpressionEditor from './jsexpressioneditor.vue';

@Component({
    components: {
        jsexpressioneditor: JSExpressionEditor
    }
})
class ExpressionInput extends Vue {
    
    @Prop({ default: '' })
    modelValue!: string;
    
    @Prop({ default: 'generic' })
    expressionType!: 'credentials' | 'recipient' | 'message' | 'command' | 'relay' | 'generic';
    
    @Prop({ default: '' })
    placeholder!: string;
    
    @Prop({ default: false })
    disabled!: boolean;
    
    showEditor = false;
    
    openEditor() {
        console.log('Opening JS expression editor, showEditor before:', this.showEditor);
        this.showEditor = true;
        console.log('Opening JS expression editor, showEditor after:', this.showEditor);
    }
    
    @Emit('update:modelValue')
    onEditorSave(value: string) {
        this.showEditor = false;
        return value;
    }
}

export default toNative(ExpressionInput);
</script>

<style scoped>
.expression-input-container {
    width: 100%;
}
</style>