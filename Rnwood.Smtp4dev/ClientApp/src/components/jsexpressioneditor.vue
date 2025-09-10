<template>
    <el-dialog 
        title="JavaScript Expression Editor" 
        :visible="visible" 
        width="80%" 
        :close-on-click-modal="false" 
        @close="handleClose"
        :destroy-on-close="true">
        
        <div class="expression-editor">
            <el-alert v-if="error" type="error" class="mb-3">
                {{ error }}
            </el-alert>
            
            <!-- Mode toggle -->
            <div class="mode-toggle mb-3">
                <el-radio-group v-model="mode" @change="onModeChange">
                    <el-radio-button label="builder">Expression Builder</el-radio-button>
                    <el-radio-button label="code">Code Editor</el-radio-button>
                </el-radio-group>
            </div>
            
            <!-- Expression Builder Mode -->
            <div v-if="mode === 'builder'" class="expression-builder">
                <expressionbuilder 
                    :expression-type="expressionType"
                    :value="builderExpression"
                    @update:value="onBuilderChange" />
            </div>
            
            <!-- Code Editor Mode -->
            <div v-if="mode === 'code'" class="code-editor">
                <div class="editor-help mb-2">
                    <el-collapse>
                        <el-collapse-item title="Available Variables and Functions" name="help">
                            <div class="help-content">
                                <h4>Context Variables:</h4>
                                <ul>
                                    <li v-for="variable in availableVariables" :key="variable.name">
                                        <strong>{{ variable.name }}</strong>: {{ variable.description }}
                                    </li>
                                </ul>
                                <h4>Available Functions:</h4>
                                <ul>
                                    <li v-for="func in availableFunctions" :key="func.name">
                                        <strong>{{ func.signature }}</strong>: {{ func.description }}
                                    </li>
                                </ul>
                            </div>
                        </el-collapse-item>
                    </el-collapse>
                </div>
                
                <aceeditor 
                    ref="aceEditor"
                    v-model:value="codeExpression" 
                    @init="editorInit" 
                    theme="chrome" 
                    lang="javascript" 
                    :options="aceOptions"
                    width="100%" 
                    height="300px"
                    :wrap="false" />
            </div>
            
            <!-- Expression preview/validation -->
            <div class="expression-preview mt-3">
                <el-card header="Expression Preview">
                    <pre><code>{{ finalExpression || '(empty)' }}</code></pre>
                </el-card>
            </div>
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
import { VAceEditor } from 'vue3-ace-editor';
import { Editor } from 'brace';
import ace from "ace-builds";
import "ace-builds/src-noconflict/mode-javascript";
import "ace-builds/src-noconflict/theme-chrome";
import "ace-builds/src-noconflict/ext-language_tools";
import ExpressionBuilder from './expressionbuilder.vue';

interface VariableInfo {
    name: string;
    description: string;
    type: string;
    properties?: VariableInfo[];
}

interface FunctionInfo {
    name: string;
    signature: string;
    description: string;
    returnType: string;
}

@Component({
    components: {
        aceeditor: VAceEditor,
        expressionbuilder: ExpressionBuilder
    }
})
class JSExpressionEditor extends Vue {
    
    @Prop({ default: false })
    visible!: boolean;
    
    @Prop({ default: '' })
    value!: string;
    
    @Prop({ default: 'generic' })
    expressionType!: 'credentials' | 'recipient' | 'message' | 'command' | 'relay' | 'generic';
    
    mode: 'builder' | 'code' = 'code';
    codeExpression: string = '';
    builderExpression: any = null;
    error: string = '';
    
    aceOptions = {
        useWorker: true,
        enableBasicAutocompletion: true,
        enableLiveAutocompletion: true,
        enableSnippets: true,
        showPrintMargin: false,
        wrap: false
    };
    
    // Standard smtp4dev functions available in all contexts
    baseFunctions: FunctionInfo[] = [
        {
            name: 'delay',
            signature: 'delay(seconds: number): boolean',
            description: 'Delays for specified seconds and returns true. If seconds is -1, delay is almost infinite.',
            returnType: 'boolean'
        },
        {
            name: 'random',
            signature: 'random(minValue: number, maxValue: number): number',
            description: 'Generates a random integer from minValue inclusive to maxValue exclusive.',
            returnType: 'number'
        },
        {
            name: 'error',
            signature: 'error(code: number, message: string): void',
            description: 'Returns a specific SMTP error and message.',
            returnType: 'void'
        },
        {
            name: 'throttle',
            signature: 'throttle(bps: number): boolean',
            description: 'Throttles connect speed to specified bits per second. Returns true.',
            returnType: 'boolean'
        },
        {
            name: 'disconnect',
            signature: 'disconnect(): void',
            description: 'Disconnects the session immediately.',
            returnType: 'void'
        }
    ];
    
    get availableVariables(): VariableInfo[] {
        const baseVariables: VariableInfo[] = [
            {
                name: 'session',
                description: 'Current SMTP session information',
                type: 'Session',
                properties: [
                    { name: 'id', description: 'Session ID', type: 'string' },
                    { name: 'startDate', description: 'Session start date', type: 'Date' },
                    { name: 'endDate', description: 'Session end date', type: 'Date' },
                    { name: 'clientAddress', description: 'Client IP address', type: 'string' },
                    { name: 'clientName', description: 'Client hostname', type: 'string' }
                ]
            }
        ];
        
        switch (this.expressionType) {
            case 'credentials':
                return [
                    ...baseVariables,
                    {
                        name: 'credentials',
                        description: 'Authentication credentials',
                        type: 'Credentials',
                        properties: [
                            { name: 'Type', description: 'Credential type (e.g., USERNAME_PASSWORD)', type: 'string' },
                            { name: 'username', description: 'Username for USERNAME_PASSWORD type', type: 'string' },
                            { name: 'password', description: 'Password for USERNAME_PASSWORD type', type: 'string' }
                        ]
                    }
                ];
                
            case 'recipient':
                return [
                    ...baseVariables,
                    {
                        name: 'recipient',
                        description: 'Current recipient email address',
                        type: 'string'
                    }
                ];
                
            case 'message':
                return [
                    ...baseVariables,
                    {
                        name: 'message',
                        description: 'Current message being processed',
                        type: 'Message',
                        properties: [
                            { name: 'id', description: 'Message ID', type: 'string' },
                            { name: 'subject', description: 'Message subject', type: 'string' },
                            { name: 'from', description: 'From address', type: 'string' },
                            { name: 'to', description: 'To addresses', type: 'string[]' },
                            { name: 'receivedDate', description: 'Date message was received', type: 'Date' }
                        ]
                    }
                ];
                
            case 'command':
                return [
                    ...baseVariables,
                    {
                        name: 'command',
                        description: 'Current SMTP command',
                        type: 'Command',
                        properties: [
                            { name: 'Verb', description: 'SMTP command verb (e.g., HELO, MAIL)', type: 'string' },
                            { name: 'ArgumentsText', description: 'Command arguments', type: 'string' }
                        ]
                    }
                ];
                
            case 'relay':
                return [
                    ...baseVariables,
                    {
                        name: 'recipient',
                        description: 'Current recipient email address',
                        type: 'string'
                    },
                    {
                        name: 'message',
                        description: 'Message to be relayed',
                        type: 'Message',
                        properties: [
                            { name: 'id', description: 'Message ID', type: 'string' },
                            { name: 'subject', description: 'Message subject', type: 'string' },
                            { name: 'from', description: 'From address', type: 'string' },
                            { name: 'to', description: 'To addresses', type: 'string[]' }
                        ]
                    }
                ];
                
            default:
                return baseVariables;
        }
    }
    
    get availableFunctions(): FunctionInfo[] {
        return this.baseFunctions;
    }
    
    get finalExpression(): string {
        if (this.mode === 'code') {
            return this.codeExpression;
        } else {
            return this.builderExpression?.expression || '';
        }
    }
    
    @Watch('value', { immediate: true })
    onValueChanged(newValue: string) {
        this.codeExpression = newValue || '';
        this.error = '';
    }
    
    @Watch('visible')
    onVisibilityChanged(newValue: boolean) {
        if (newValue) {
            this.error = '';
            // Try to determine if the expression is simple enough for the builder
            if (this.isSimpleExpression(this.codeExpression)) {
                this.mode = 'builder';
                this.parseExpressionForBuilder();
            } else {
                this.mode = 'code';
            }
        }
    }
    
    editorInit(editor: Editor) {
        // Set up custom autocompletion
        const customCompleter = {
            getCompletions: (editor: any, session: any, pos: any, prefix: any, callback: any) => {
                const completions = [];
                
                // Add variables
                for (const variable of this.availableVariables) {
                    completions.push({
                        caption: variable.name,
                        value: variable.name,
                        meta: variable.type,
                        type: 'variable',
                        score: 1000
                    });
                    
                    // Add properties if available
                    if (variable.properties) {
                        for (const prop of variable.properties) {
                            completions.push({
                                caption: `${variable.name}.${prop.name}`,
                                value: `${variable.name}.${prop.name}`,
                                meta: prop.type,
                                type: 'property',
                                score: 900
                            });
                        }
                    }
                }
                
                // Add functions
                for (const func of this.availableFunctions) {
                    completions.push({
                        caption: func.name,
                        value: func.signature.split(':')[0], // Get just the function call part
                        meta: func.returnType,
                        type: 'function',
                        score: 800,
                        docText: func.description
                    });
                }
                
                callback(null, completions);
            }
        };
        
        // Register the custom completer
        const ace = (window as any).ace || require('ace-builds');
        ace.require('ace/ext/language_tools').addCompleter(customCompleter);
        
        editor.setShowPrintMargin(false);
    }
    
    onModeChange() {
        if (this.mode === 'builder') {
            this.parseExpressionForBuilder();
        }
    }
    
    onBuilderChange(value: any) {
        this.builderExpression = value;
    }
    
    parseExpressionForBuilder() {
        // TODO: Parse simple expressions for the builder
        this.builderExpression = { expression: this.codeExpression };
    }
    
    isSimpleExpression(expr: string): boolean {
        // Simple heuristic: if it contains complex JS constructs, it's not simple
        const complexPatterns = [
            /function\s*\(/,
            /=>/,
            /for\s*\(/,
            /while\s*\(/,
            /if\s*\(/,
            /\{[\s\S]*\}/
        ];
        
        return !complexPatterns.some(pattern => pattern.test(expr));
    }
    
    @Emit('update:value')
    handleSave() {
        const expression = this.finalExpression;
        this.handleClose();
        return expression;
    }
    
    @Emit('close')
    handleClose() {
        return;
    }
}

export default toNative(JSExpressionEditor);
</script>

<style scoped>
.expression-editor {
    min-height: 400px;
}

.mode-toggle {
    text-align: center;
}

.editor-help {
    max-height: 200px;
    overflow-y: auto;
}

.help-content ul {
    margin: 0;
    padding-left: 20px;
}

.help-content li {
    margin-bottom: 5px;
}

.expression-preview {
    background: #f5f5f5;
    border-radius: 4px;
    padding: 10px;
}

.expression-preview pre {
    margin: 0;
    white-space: pre-wrap;
    word-break: break-word;
}

.mb-2 {
    margin-bottom: 8px;
}

.mb-3 {
    margin-bottom: 16px;
}

.mt-3 {
    margin-top: 16px;
}
</style>