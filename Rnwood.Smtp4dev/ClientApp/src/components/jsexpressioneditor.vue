<template>
    <el-dialog 
        title="JavaScript Expression Editor" 
        v-model="dialogVisible" 
        width="80%" 
        :close-on-click-modal="false" 
        :destroy-on-close="true">
        
        <div class="expression-editor">
            <p>JavaScript Expression Editor - Test Version</p>
            <p>Expression Type: {{ expressionType }}</p>
            <p>Current Value: {{ value }}</p>
            
            <el-input 
                v-model="testExpression" 
                placeholder="Enter test expression..."
                rows="3"
                type="textarea">
            </el-input>
        </div>
        
        <template #footer>
            <div class="dialog-footer">
                <el-button @click="handleClose">Cancel</el-button>
                <el-button type="primary" @click="handleSave">Save</el-button>
            </div>
        </template>
    </el-dialog>
</template>

<script lang="ts">
import { Component, Vue, Prop, Emit, Watch, toNative } from 'vue-facing-decorator';

@Component
class JSExpressionEditor extends Vue {
    
    @Prop({ default: false })
    visible!: boolean;
    
    @Prop({ default: '' })
    value!: string;
    
    @Prop({ default: 'generic' })
    expressionType!: string;
    
    testExpression: string = '';
    
    get dialogVisible(): boolean {
        return this.visible;
    }
    
    set dialogVisible(value: boolean) {
        if (value !== this.visible) {
            this.$emit('update:visible', value);
        }
    }
    
    @Watch('visible')
    onVisibleChanged(newValue: boolean) {
        console.log('Dialog visibility changed to:', newValue);
    }
    
    mounted() {
        this.testExpression = this.value;
        console.log('JSExpressionEditor mounted, visible:', this.visible);
    }
    
    @Emit('update:value')
    handleSave() {
        console.log('Saving expression:', this.testExpression);
        this.dialogVisible = false;
        return this.testExpression;
    }
    
    @Emit('close')
    handleClose() {
        console.log('Closing dialog');
        this.dialogVisible = false;
        return;
    }
}

export default toNative(JSExpressionEditor);
</script>

<style scoped>
.expression-editor {
    min-height: 200px;
    padding: 20px;
}
</style>