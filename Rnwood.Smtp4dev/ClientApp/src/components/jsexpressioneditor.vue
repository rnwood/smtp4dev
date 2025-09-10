<template>
    <el-dialog 
        title="JavaScript Expression Editor" 
        v-model="dialogVisible" 
        width="90%" 
        :close-on-click-modal="false" 
        :destroy-on-close="true"
        class="expression-editor-dialog">
        
        <div class="expression-editor">
            <div class="editor-header">
                <div class="header-info">
                    <h4>{{ getExpressionTypeTitle(expressionType) }}</h4>
                    <p class="expression-description">{{ getExpressionDescription(expressionType) }}</p>
                </div>
                
                <el-tabs v-model="activeTab" class="editor-tabs">
                    <el-tab-pane label="Code Editor" name="editor">
                        <div class="ace-editor-container">
                            <aceeditor 
                                v-model:value="editorExpression"
                                @init="editorInit"
                                theme="monokai"
                                lang="javascript"
                                :options="{ 
                                    useWorker: false,
                                    enableLiveAutocompletion: true,
                                    enableSnippets: true,
                                    showPrintMargin: false,
                                    fontSize: 14,
                                    wrap: true
                                }"
                                width="100%" 
                                height="300px"
                                :placeholder="getCodeEditorPlaceholder()"
                                @input="onEditorChange"
                            />
                        </div>
                        
                        <div class="editor-toolbar">
                            <div class="toolbar-left">
                                <el-button size="small" @click="insertTemplate" icon="plus">Insert Template</el-button>
                                <el-button size="small" @click="validateExpression" icon="check">Validate</el-button>
                                <el-button size="small" @click="testExpression" icon="play">Test</el-button>
                            </div>
                            <div class="toolbar-right">
                                <el-button size="small" @click="showHelpDialog = true" icon="question">Help</el-button>
                            </div>
                        </div>
                    </el-tab-pane>
                    
                    <el-tab-pane label="Visual Builder" name="builder">
                        <div class="builder-container">
                            <expressionbuilder 
                                :expression-type="expressionType"
                                :value="builderValue"
                                @update:value="onBuilderChange"
                            />
                        </div>
                    </el-tab-pane>
                </el-tabs>
                
                <!-- Validation Results -->
                <div v-if="validationResult" class="validation-result" :class="validationResult.valid ? 'success' : 'error'">
                    <el-icon v-if="validationResult.valid" class="success-icon"><CircleCheck /></el-icon>
                    <el-icon v-else class="error-icon"><CircleClose /></el-icon>
                    <span>{{ validationResult.message }}</span>
                </div>
                
                <!-- Test Results -->
                <div v-if="testResult" class="test-result">
                    <h5>Test Result:</h5>
                    <el-card>
                        <pre>{{ testResult }}</pre>
                    </el-card>
                </div>
            </div>
        </div>
        
        <template #footer>
            <div class="dialog-footer">
                <el-button @click="handleClose">Cancel</el-button>
                <el-button type="primary" @click="handleSave" :disabled="!isValidExpression">Save Expression</el-button>
            </div>
        </template>
        
        <!-- Help Dialog -->
        <el-dialog
            title="Expression Help"
            v-model="showHelpDialog"
            width="70%"
            append-to-body>
            <div class="help-content">
                <div class="help-section">
                    <h4>Available Variables for {{ getExpressionTypeTitle(expressionType) }}</h4>
                    <el-table :data="getAvailableVariables()" style="width: 100%">
                        <el-table-column prop="name" label="Variable" width="200"></el-table-column>
                        <el-table-column prop="type" label="Type" width="100"></el-table-column>
                        <el-table-column prop="description" label="Description"></el-table-column>
                    </el-table>
                </div>
                
                <div class="help-section">
                    <h4>Example Expressions</h4>
                    <div v-for="example in getExampleExpressions()" :key="example.title" class="example">
                        <h5>{{ example.title }}</h5>
                        <pre><code>{{ example.code }}</code></pre>
                        <p>{{ example.description }}</p>
                    </div>
                </div>
            </div>
        </el-dialog>
    </el-dialog>
</template>

<script lang="ts">
import { Component, Vue, Prop, Emit, Watch, toNative } from 'vue-facing-decorator';
import { CircleCheck, CircleClose } from '@element-plus/icons-vue';
import ExpressionBuilder from './expressionbuilder.vue';

import { VAceEditor } from 'vue3-ace-editor';
import { Editor } from 'brace';
import "ace-builds/src-noconflict/mode-javascript";
import "ace-builds/src-noconflict/theme-monokai";
import "ace-builds/src-noconflict/theme-chrome";
import "ace-builds/src-noconflict/ext-language_tools";

// Configure ACE base path for Vite
import ace from 'ace-builds/src-noconflict/ace';
ace.config.set('basePath', '/node_modules/ace-builds/src-noconflict/');

interface ValidationResult {
    valid: boolean;
    message: string;
}

interface Variable {
    name: string;
    type: string;
    description: string;
}

interface Example {
    title: string;
    code: string;
    description: string;
}

@Component({
    components: {
        CircleCheck,
        CircleClose,
        expressionbuilder: ExpressionBuilder,
        aceeditor: VAceEditor
    }
})
class JSExpressionEditor extends Vue {
    
    @Prop({ default: false })
    visible!: boolean;
    
    @Prop({ default: '' })
    value!: string;
    
    @Prop({ default: 'generic' })
    expressionType!: string;
    
    editorExpression: string = '';
    activeTab: string = 'editor';
    showHelpDialog: boolean = false;
    validationResult: ValidationResult | null = null;
    testResult: string = '';
    builderValue: any = null;
    
    get dialogVisible(): boolean {
        return this.visible;
    }
    
    set dialogVisible(value: boolean) {
        if (value !== this.visible) {
            this.$emit('update:visible', value);
        }
    }
    
    get isValidExpression(): boolean {
        return !this.validationResult || this.validationResult.valid;
    }
    
    @Watch('visible')
    onVisibleChanged(newValue: boolean) {
        if (newValue) {
            this.editorExpression = this.value;
            this.validationResult = null;
            this.testResult = '';
            this.activeTab = 'editor';
        }
    }
    
    mounted() {
        this.editorExpression = this.value;
    }
    
    editorInit(editor: Editor) {
        // Configure ACE editor for JavaScript expressions
        editor.setOptions({
            enableBasicAutocompletion: true,
            enableLiveAutocompletion: true,
            enableSnippets: true,
            showLineNumbers: true,
            showGutter: true,
            fontSize: 14,
            wrap: true,
            scrollPastEnd: true
        });

        // Add custom completions for smtp4dev expressions
        const langTools = (window as any).ace.require('ace/ext/language_tools');
        const customCompleter = {
            getCompletions: (editor: any, session: any, pos: any, prefix: any, callback: any) => {
                const completions = this.getAutocompletions();
                callback(null, completions.map(comp => ({
                    caption: comp.caption,
                    snippet: comp.snippet,
                    meta: comp.meta,
                    docText: comp.docText,
                    score: 1000 // High priority for our custom completions
                })));
            }
        };
        
        langTools.addCompleter(customCompleter);
        
        // Set theme based on user preference or default
        editor.setTheme('ace/theme/monokai');
        
        // Configure for single line expressions if needed
        editor.setOption('maxLines', 15);
        editor.setOption('minLines', 5);
    }
    
    getCodeEditorPlaceholder(): string {
        switch (this.expressionType) {
            case 'credentials': return 'Enter JavaScript expression to validate credentials (e.g., credentials.username === "admin")';
            case 'recipient': return 'Enter JavaScript expression to validate recipients (e.g., recipient.endsWith("@example.com"))';
            case 'message': return 'Enter JavaScript expression to validate messages (e.g., !message.subject.includes("spam"))';
            case 'command': return 'Enter JavaScript expression to validate commands (e.g., command.Verb === "HELO")';
            case 'relay': return 'Enter JavaScript expression for auto relay logic (e.g., recipient.replace("@test.com", "@prod.com"))';
            default: return 'Enter JavaScript expression...';
        }
    }
    
    onEditorChange(value: string) {
        this.editorExpression = value;
        this.validationResult = null; // Clear validation when editing
    }
    
    onBuilderChange(builderData: any) {
        this.builderValue = builderData;
        if (builderData && builderData.expression) {
            this.editorExpression = builderData.expression;
            // Switch to editor tab to show the generated code
            this.activeTab = 'editor';
        }
    }
    
    getAutocompletions() {
        const completions: any[] = [];
        
        // Add JavaScript keywords
        const jsKeywords = ['true', 'false', 'null', 'undefined', 'return', 'if', 'else', 'for', 'while', 'function'];
        jsKeywords.forEach(keyword => {
            completions.push({
                caption: keyword,
                snippet: keyword,
                meta: 'keyword'
            });
        });
        
        // Add expression type specific variables
        const variables = this.getAvailableVariables();
        variables.forEach(variable => {
            completions.push({
                caption: variable.name,
                snippet: variable.name,
                meta: variable.type,
                docText: variable.description
            });
        });
        
        // Add common functions
        const functions = [
            { name: 'includes()', desc: 'Check if string contains substring' },
            { name: 'startsWith()', desc: 'Check if string starts with substring' },
            { name: 'endsWith()', desc: 'Check if string ends with substring' },
            { name: 'match()', desc: 'Match against regular expression' },
            { name: 'toLowerCase()', desc: 'Convert to lowercase' },
            { name: 'toUpperCase()', desc: 'Convert to uppercase' },
            { name: 'trim()', desc: 'Remove whitespace from both ends' }
        ];
        
        functions.forEach(func => {
            completions.push({
                caption: func.name,
                snippet: func.name,
                meta: 'function',
                docText: func.desc
            });
        });
        
        return completions;
    }
    
    getExpressionTypeTitle(type: string): string {
        switch (type) {
            case 'credentials': return 'Credentials Validation';
            case 'recipient': return 'Recipient Validation';
            case 'message': return 'Message Validation';
            case 'command': return 'Command Validation';
            case 'relay': return 'Auto Relay Logic';
            default: return 'Expression Editor';
        }
    }
    
    getExpressionDescription(type: string): string {
        switch (type) {
            case 'credentials': return 'Validate SMTP authentication credentials before allowing access.';
            case 'recipient': return 'Control which recipients are accepted by the SMTP server.';
            case 'message': return 'Filter messages based on content, headers, or other properties.';
            case 'command': return 'Validate SMTP commands before they are processed.';
            case 'relay': return 'Define logic for automatically relaying messages to external servers.';
            default: return 'Create JavaScript expressions for smtp4dev functionality.';
        }
    }
    
    getAvailableVariables(): Variable[] {
        const commonVars: Variable[] = [
            { name: 'session.clientAddress', type: 'string', description: 'IP address of the connecting client' },
            { name: 'session.clientName', type: 'string', description: 'Hostname of the connecting client' },
            { name: 'session.id', type: 'string', description: 'Unique session identifier' }
        ];
        
        switch (this.expressionType) {
            case 'credentials':
                return [
                    ...commonVars,
                    { name: 'credentials.Type', type: 'string', description: 'Authentication type (e.g., USERNAME_PASSWORD)' },
                    { name: 'credentials.username', type: 'string', description: 'Provided username' },
                    { name: 'credentials.password', type: 'string', description: 'Provided password' }
                ];
                
            case 'recipient':
                return [
                    ...commonVars,
                    { name: 'recipient', type: 'string', description: 'Email address of the recipient' }
                ];
                
            case 'message':
                return [
                    ...commonVars,
                    { name: 'message.subject', type: 'string', description: 'Subject line of the message' },
                    { name: 'message.from', type: 'string', description: 'From address of the message' },
                    { name: 'message.id', type: 'string', description: 'Unique message identifier' },
                    { name: 'message.size', type: 'number', description: 'Size of the message in bytes' }
                ];
                
            case 'command':
                return [
                    ...commonVars,
                    { name: 'command.Verb', type: 'string', description: 'SMTP command verb (HELO, MAIL, RCPT, etc.)' },
                    { name: 'command.ArgumentsText', type: 'string', description: 'Arguments provided with the command' }
                ];
                
            case 'relay':
                return [
                    ...commonVars,
                    { name: 'recipient', type: 'string', description: 'Email address of the recipient' },
                    { name: 'message.subject', type: 'string', description: 'Subject line of the message' },
                    { name: 'message.from', type: 'string', description: 'From address of the message' }
                ];
                
            default:
                return commonVars;
        }
    }
    
    getExampleExpressions(): Example[] {
        switch (this.expressionType) {
            case 'credentials':
                return [
                    {
                        title: 'Simple Username/Password Check',
                        code: "credentials.Type == 'USERNAME_PASSWORD' && credentials.username == 'admin' && credentials.password == 'secret'",
                        description: 'Accept only specific username and password combination'
                    },
                    {
                        title: 'Multiple Valid Users',
                        code: "credentials.Type == 'USERNAME_PASSWORD' && ['admin', 'user1', 'user2'].includes(credentials.username)",
                        description: 'Accept multiple valid usernames with any password'
                    }
                ];
                
            case 'recipient':
                return [
                    {
                        title: 'Domain Whitelist',
                        code: "recipient.endsWith('@example.com') || recipient.endsWith('@test.com')",
                        description: 'Only allow recipients from specific domains'
                    },
                    {
                        title: 'Block Spam Domains',
                        code: "!recipient.endsWith('@spam.com') && !recipient.includes('noreply')",
                        description: 'Block specific domains and noreply addresses'
                    }
                ];
                
            case 'message':
                return [
                    {
                        title: 'Subject Filter',
                        code: "!message.subject.toLowerCase().includes('spam') && !message.subject.includes('[BULK]')",
                        description: 'Reject messages with spam indicators in subject'
                    },
                    {
                        title: 'Size Limit',
                        code: "message.size < 1048576",
                        description: 'Reject messages larger than 1MB'
                    }
                ];
                
            case 'command':
                return [
                    {
                        title: 'HELO Validation',
                        code: "command.Verb == 'HELO' && command.ArgumentsText.length > 0 && !command.ArgumentsText.includes('localhost')",
                        description: 'Require valid HELO arguments, reject localhost'
                    }
                ];
                
            case 'relay':
                return [
                    {
                        title: 'Domain Rewriting',
                        code: "recipient.replace(/@test.com$/, '@production.com')",
                        description: 'Rewrite test domain to production domain'
                    },
                    {
                        title: 'Conditional Relay',
                        code: "message.subject.includes('[RELAY]') ? recipient : null",
                        description: 'Only relay messages with [RELAY] in subject'
                    }
                ];
                
            default:
                return [];
        }
    }
    
    insertTemplate() {
        const examples = this.getExampleExpressions();
        if (examples.length > 0) {
            this.editorExpression = examples[0].code;
        }
    }
    
    validateExpression() {
        try {
            // Basic syntax validation using Function constructor
            new Function('return ' + this.editorExpression);
            this.validationResult = {
                valid: true,
                message: 'Expression syntax is valid'
            };
        } catch (error: any) {
            this.validationResult = {
                valid: false,
                message: `Syntax error: ${error.message}`
            };
        }
    }
    
    testExpression() {
        try {
            // Create a test environment with sample data
            const testData = this.getTestData();
            const func = new Function(...Object.keys(testData), 'return ' + this.editorExpression);
            const result = func(...Object.values(testData));
            this.testResult = JSON.stringify(result, null, 2);
        } catch (error: any) {
            this.testResult = `Error: ${error.message}`;
        }
    }
    
    getTestData(): any {
        const commonData = {
            session: {
                clientAddress: '127.0.0.1',
                clientName: 'localhost',
                id: 'test-session-123'
            }
        };
        
        switch (this.expressionType) {
            case 'credentials':
                return {
                    ...commonData,
                    credentials: {
                        Type: 'USERNAME_PASSWORD',
                        username: 'testuser',
                        password: 'testpass'
                    }
                };
                
            case 'recipient':
                return {
                    ...commonData,
                    recipient: 'test@example.com'
                };
                
            case 'message':
                return {
                    ...commonData,
                    message: {
                        subject: 'Test Message',
                        from: 'sender@example.com',
                        id: 'msg-123',
                        size: 1024
                    }
                };
                
            case 'command':
                return {
                    ...commonData,
                    command: {
                        Verb: 'HELO',
                        ArgumentsText: 'client.example.com'
                    }
                };
                
            case 'relay':
                return {
                    ...commonData,
                    recipient: 'user@test.com',
                    message: {
                        subject: 'Test Message',
                        from: 'sender@example.com'
                    }
                };
                
            default:
                return commonData;
        }
    }
    
    @Emit('update:value')
    handleSave() {
        const expression = this.activeTab === 'builder' && this.builderValue 
            ? this.builderValue.expression 
            : this.editorExpression;
            
        this.dialogVisible = false;
        return expression;
    }
    
    @Emit('close')
    handleClose() {
        this.dialogVisible = false;
        return;
    }
}

export default toNative(JSExpressionEditor);
</script>

<style scoped>
.expression-editor-dialog :deep(.el-dialog) {
    min-height: 70vh;
}

.expression-editor {
    min-height: 500px;
}

.editor-header {
    margin-bottom: 20px;
}

.header-info h4 {
    margin: 0 0 8px 0;
    color: #303133;
}

.expression-description {
    margin: 0 0 16px 0;
    color: #606266;
    font-size: 14px;
}

.editor-tabs {
    margin-bottom: 16px;
}

.ace-editor-container {
    border: 1px solid #dcdfe6;
    border-radius: 4px;
    overflow: hidden;
    margin-bottom: 16px;
}

.ace-editor-container :deep(.ace_editor) {
    font-size: 14px !important;
    font-family: 'Monaco', 'Consolas', 'Courier New', monospace !important;
    border-radius: 4px;
}

.ace-editor-container :deep(.ace_content) {
    background: #2d3748;
}

.ace-editor-container :deep(.ace_gutter) {
    background: #2d3748;
    border-right: 1px solid #4a5568;
}

.ace-editor-container :deep(.ace_scroller) {
    background: #2d3748;
}

.code-editor-container {
    margin-bottom: 16px;
}

.code-textarea {
    font-family: 'Monaco', 'Consolas', 'Courier New', monospace;
    font-size: 14px;
}

.code-textarea :deep(.el-textarea__inner) {
    font-family: 'Monaco', 'Consolas', 'Courier New', monospace;
    font-size: 14px;
    line-height: 1.4;
}

.editor-toolbar {
    display: flex;
    justify-content: space-between;
    align-items: center;
    margin-top: 12px;
    padding: 8px 0;
    border-top: 1px solid #ebeef5;
}

.toolbar-left,
.toolbar-right {
    display: flex;
    gap: 8px;
}

.builder-container {
    min-height: 300px;
    border: 1px solid #dcdfe6;
    border-radius: 4px;
    padding: 16px;
    background: #fafafa;
}

.validation-result {
    display: flex;
    align-items: center;
    gap: 8px;
    margin-top: 12px;
    padding: 8px 12px;
    border-radius: 4px;
    font-size: 14px;
}

.validation-result.success {
    background: #f0f9ff;
    color: #067f8c;
    border: 1px solid #91d5ff;
}

.validation-result.error {
    background: #fff2f0;
    color: #cf1322;
    border: 1px solid #ffccc7;
}

.success-icon {
    color: #52c41a;
}

.error-icon {
    color: #ff4d4f;
}

.test-result {
    margin-top: 16px;
}

.test-result h5 {
    margin: 0 0 8px 0;
}

.test-result pre {
    margin: 0;
    white-space: pre-wrap;
    word-break: break-word;
    font-family: 'Monaco', 'Consolas', monospace;
    font-size: 12px;
}

.help-content {
    max-height: 60vh;
    overflow-y: auto;
}

.help-section {
    margin-bottom: 24px;
}

.help-section h4 {
    margin: 0 0 12px 0;
    color: #303133;
}

.example {
    margin-bottom: 16px;
    padding: 12px;
    background: #f5f5f5;
    border-radius: 4px;
}

.example h5 {
    margin: 0 0 8px 0;
    color: #606266;
}

.example pre {
    margin: 8px 0;
    padding: 8px;
    background: #fff;
    border: 1px solid #ebeef5;
    border-radius: 4px;
    font-size: 12px;
    overflow-x: auto;
}

.example p {
    margin: 8px 0 0 0;
    color: #909399;
    font-size: 13px;
}

.dialog-footer {
    display: flex;
    justify-content: flex-end;
    gap: 12px;
}
</style>