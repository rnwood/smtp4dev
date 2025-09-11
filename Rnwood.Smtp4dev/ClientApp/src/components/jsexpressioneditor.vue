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
                    <div class="return-values-info">
                        <strong>Return Values:</strong> {{ getReturnValueDescription(expressionType) }}
                    </div>
                </div>
            </div>
            
            <div class="editor-main">
                <div class="editor-content">
                    <div class="ace-editor-container">
                        <aceeditor 
                            v-model:value="editorExpression"
                            @init="editorInit"
                            ref="aceEditor"
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
                            height="100%"
                            :placeholder="getCodeEditorPlaceholder()"
                        />
                    </div>
                    
                    <div class="editor-toolbar">
                        <div class="toolbar-left">
                            <el-button size="small" @click="validateExpression" icon="check">Validate</el-button>
                        </div>
                    </div>
                    
                    <!-- Validation Results -->
                    <div v-if="validationResult" class="validation-result" :class="validationResult.valid ? 'success' : 'error'">
                        <el-icon v-if="validationResult.valid" class="success-icon"><CircleCheck /></el-icon>
                        <el-icon v-else class="error-icon"><CircleClose /></el-icon>
                        <span>{{ validationResult.message }}</span>
                    </div>
                </div>
                
                <!-- Help Panel -->
                <div class="help-panel">
                    <div class="help-content">
                        <!-- Available Variables Section -->
                        <div class="help-section">
                            <div class="section-header" @click="toggleSection('variables')">
                                <el-icon class="expand-icon" :class="{ expanded: helpSections.variables }">
                                    <CaretRight />
                                </el-icon>
                                <h4>Available Variables</h4>
                            </div>
                            <div v-if="helpSections.variables" class="section-content">
                                <div v-for="variable in getTopLevelVariables()" :key="variable.name" class="variable-section">
                                    <div class="variable-header" @click="toggleVariableExpansion(variable)">
                                        <el-icon class="expand-icon" :class="{ expanded: variable.expanded }">
                                            <CaretRight />
                                        </el-icon>
                                        <span class="variable-name clickable-item" @click.stop="insertAtCursor(variable.name)">{{ variable.name }}</span>
                                        <span class="variable-type">{{ variable.type }}</span>
                                        <span class="variable-description">{{ variable.description }}</span>
                                    </div>
                                    <div v-if="variable.expanded && variable.properties && variable.properties.length > 0" class="variable-properties">
                                        <el-table :data="variable.properties" style="width: 100%" size="small">
                                            <el-table-column prop="name" label="Property" width="150">
                                                <template #default="scope">
                                                    <span class="clickable-item" @click="insertAtCursor(variable.name + '.' + scope.row.name)">{{ scope.row.name }}</span>
                                                </template>
                                            </el-table-column>
                                            <el-table-column prop="type" label="Type" width="80"></el-table-column>
                                            <el-table-column prop="description" label="Description"></el-table-column>
                                        </el-table>
                                    </div>
                                </div>
                            </div>
                        </div>
                        
                        <!-- Standard API Functions Section -->
                        <div class="help-section">
                            <div class="section-header" @click="toggleSection('functions')">
                                <el-icon class="expand-icon" :class="{ expanded: helpSections.functions }">
                                    <CaretRight />
                                </el-icon>
                                <h4>Standard API Functions</h4>
                            </div>
                            <div v-if="helpSections.functions" class="section-content">
                                <el-table :data="getStandardApiFunctions()" style="width: 100%" size="small">
                                    <el-table-column prop="name" label="Function" width="200">
                                        <template #default="scope">
                                            <span class="clickable-item" @click="insertAtCursor(scope.row.name)">{{ scope.row.name }}</span>
                                        </template>
                                    </el-table-column>
                                    <el-table-column prop="type" label="Returns" width="80"></el-table-column>
                                    <el-table-column prop="description" label="Description"></el-table-column>
                                </el-table>
                            </div>
                        </div>
                        
                        <!-- JavaScript Operators Section -->
                        <div class="help-section">
                            <div class="section-header" @click="toggleSection('operators')">
                                <el-icon class="expand-icon" :class="{ expanded: helpSections.operators }">
                                    <CaretRight />
                                </el-icon>
                                <h4>JavaScript Operators</h4>
                            </div>
                            <div v-if="helpSections.operators" class="section-content">
                                <el-table :data="getJavaScriptOperators()" style="width: 100%" size="small">
                                    <el-table-column prop="operator" label="Operator" width="80">
                                        <template #default="scope">
                                            <span class="clickable-item operator-text" @click="insertAtCursor(scope.row.operator)">{{ scope.row.operator }}</span>
                                        </template>
                                    </el-table-column>
                                    <el-table-column prop="name" label="Name" width="120"></el-table-column>
                                    <el-table-column prop="description" label="Description"></el-table-column>
                                </el-table>
                            </div>
                        </div>
                        
                        <!-- Example Expressions Section -->
                        <div class="help-section">
                            <div class="section-header" @click="toggleSection('examples')">
                                <el-icon class="expand-icon" :class="{ expanded: helpSections.examples }">
                                    <CaretRight />
                                </el-icon>
                                <h4>Example Expressions</h4>
                            </div>
                            <div v-if="helpSections.examples" class="section-content">
                                <div v-for="example in getExampleExpressions()" :key="example.title" class="example">
                                    <h5>{{ example.title }}</h5>
                                    <pre class="clickable-item" @click="insertAtCursor(example.code)"><code>{{ example.code }}</code></pre>
                                    <p>{{ example.description }}</p>
                                </div>
                            </div>
                        </div>
                    </div>
                </div>
            </div>
        </div>
        
        <template #footer>
            <div class="dialog-footer">
                <el-button @click="handleClose">Cancel</el-button>
                <el-button type="primary" @click="handleSave" :disabled="!isValidExpression">Save Expression</el-button>
            </div>
        </template>
    </el-dialog>
</template>

<script lang="ts">
import { Component, Vue, Prop, Emit, Watch, toNative } from 'vue-facing-decorator';
import { CircleCheck, CircleClose, CaretRight } from '@element-plus/icons-vue';

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

interface TopLevelVariable {
    name: string;
    type: string;
    description: string;
    expanded?: boolean;
    properties?: Variable[];
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
        CaretRight,
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
    validationResult: ValidationResult | null = null;
    topLevelVariables: TopLevelVariable[] = [];
    aceEditorInstance: any = null;
    helpSections = {
        variables: false,
        functions: false,
        operators: false,
        examples: false
    };
    
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
            this.topLevelVariables = this.buildTopLevelVariables();
        }
    }
    
    @Watch('expressionType')
    onExpressionTypeChanged() {
        this.topLevelVariables = this.buildTopLevelVariables();
    }
    
    mounted() {
        this.editorExpression = this.value;
        this.topLevelVariables = this.buildTopLevelVariables();
    }
    
    editorInit(editor: Editor) {
        // Store reference to the editor instance
        this.aceEditorInstance = editor;
        
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
        
        // Add smtp4dev specific functions
        const smtp4devFunctions = [
            { name: 'delay(seconds)', desc: 'Delays for specified seconds and returns true. Use -1 for infinite delay.', type: 'function' },
            { name: 'random(min, max)', desc: 'Generates random integer from min (inclusive) to max (exclusive)', type: 'function' },
            { name: 'error(code, message)', desc: 'Returns specific SMTP error code and message', type: 'function' },
            { name: 'throttle(bps)', desc: 'Throttles connection speed to specified bits per second', type: 'function' },
            { name: 'disconnect()', desc: 'Disconnects the session immediately', type: 'function' }
        ];

        smtp4devFunctions.forEach(func => {
            completions.push({
                caption: func.name,
                snippet: func.name,
                meta: func.type,
                docText: func.desc
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
    
    getReturnValueDescription(type: string): string {
        switch (type) {
            case 'credentials': return 'Return true to accept credentials, false to reject.';
            case 'recipient': return 'Return true to accept recipient, false to reject.';
            case 'message': return 'Return true to accept, false to reject, number for error code, or string for error message.';
            case 'command': return 'Return true to accept, false to reject, number for error code, or string for error message.';
            case 'relay': return 'Return string/array of recipients to relay to, true for original recipient, false/null to not relay.';
            default: return 'Return appropriate value based on expression type.';
        }
    }
    
    buildTopLevelVariables(): TopLevelVariable[] {
        const sessionProperties: Variable[] = [
            { name: 'id', type: 'string', description: 'Unique session identifier' },
            { name: 'startDate', type: 'Date', description: 'When the session started' },
            { name: 'clientAddress', type: 'string', description: 'IP address of the connecting client' },
            { name: 'clientName', type: 'string', description: 'Hostname of the connecting client' },
            { name: 'error', type: 'string', description: 'Session error message if any' },
            { name: 'errorType', type: 'string', description: 'Session error type if any' }
        ];

        const messageProperties: Variable[] = [
            { name: 'id', type: 'string', description: 'Unique message identifier' },
            { name: 'from', type: 'string', description: 'From address of the message' },
            { name: 'to', type: 'string[]', description: 'Array of To addresses' },
            { name: 'cc', type: 'string[]', description: 'Array of CC addresses' },
            { name: 'bcc', type: 'string[]', description: 'Array of BCC addresses' },
            { name: 'deliveredTo', type: 'string[]', description: 'Array of addresses message was delivered to' },
            { name: 'subject', type: 'string', description: 'Subject line of the message' },
            { name: 'receivedDate', type: 'Date', description: 'When the message was received' },
            { name: 'secureConnection', type: 'boolean', description: 'Whether connection used TLS/SSL' },
            { name: 'hasHtmlBody', type: 'boolean', description: 'Whether message has HTML body' },
            { name: 'hasPlainTextBody', type: 'boolean', description: 'Whether message has plain text body' },
            { name: 'headers', type: 'Header[]', description: 'Array of message headers' },
            { name: 'parts', type: 'MessagePart[]', description: 'Array of message parts/attachments' },
            { name: 'mimeParseError', type: 'string', description: 'MIME parsing error if any' },
            { name: 'relayError', type: 'string', description: 'Relay error if any' }
        ];

        const commandProperties: Variable[] = [
            { name: 'verb', type: 'string', description: 'SMTP command verb (HELO, MAIL, RCPT, etc.)' },
            { name: 'argumentsText', type: 'string', description: 'Arguments provided with the command' },
            { name: 'text', type: 'string', description: 'Full command text as received' },
            { name: 'isValid', type: 'boolean', description: 'Whether command syntax is valid' },
            { name: 'isEmpty', type: 'boolean', description: 'Whether command is empty' }
        ];

        const credentialsProperties: Variable[] = [
            { name: 'type', type: 'string', description: 'Authentication type (e.g., USERNAME_PASSWORD)' },
            { name: 'username', type: 'string', description: 'Provided username (for USERNAME_PASSWORD type)' },
            { name: 'password', type: 'string', description: 'Provided password (for USERNAME_PASSWORD type)' }
        ];

        const baseVariables: TopLevelVariable[] = [
            {
                name: 'session',
                type: 'object',
                description: 'Current SMTP session information',
                expanded: false,
                properties: sessionProperties
            }
        ];

        switch (this.expressionType) {
            case 'credentials':
                return [
                    {
                        name: 'credentials',
                        type: 'object',
                        description: 'Authentication credentials provided by client',
                        expanded: false,
                        properties: credentialsProperties
                    },
                    ...baseVariables
                ];

            case 'recipient':
                return [
                    {
                        name: 'recipient',
                        type: 'string',
                        description: 'Email address of the recipient being validated',
                        expanded: false,
                        properties: []
                    },
                    ...baseVariables
                ];

            case 'message':
                return [
                    {
                        name: 'message',
                        type: 'object',
                        description: 'Email message being processed',
                        expanded: false,
                        properties: messageProperties
                    },
                    ...baseVariables
                ];

            case 'command':
                return [
                    {
                        name: 'command',
                        type: 'object',
                        description: 'SMTP command being processed',
                        expanded: false,
                        properties: commandProperties
                    },
                    ...baseVariables
                ];

            case 'relay':
                return [
                    {
                        name: 'recipient',
                        type: 'string',
                        description: 'Original recipient email address',
                        expanded: false,
                        properties: []
                    },
                    {
                        name: 'message',
                        type: 'object',
                        description: 'Email message to be relayed',
                        expanded: false,
                        properties: messageProperties
                    },
                    ...baseVariables
                ];

            default:
                return baseVariables;
        }
    }
    
    getTopLevelVariables(): TopLevelVariable[] {
        return this.topLevelVariables;
    }
    
    toggleVariableExpansion(variable: TopLevelVariable) {
        variable.expanded = !variable.expanded;
    }
    
    toggleSection(sectionName: keyof typeof this.helpSections) {
        this.helpSections[sectionName] = !this.helpSections[sectionName];
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
    
    getStandardApiFunctions(): Variable[] {
        return [
            { name: 'delay(seconds)', type: 'boolean', description: 'Delays for specified seconds and returns true. If seconds is -1, delay is almost infinite.' },
            { name: 'random(minValue, maxValue)', type: 'number', description: 'Generates a random integer from minValue inclusive to maxValue exclusive.' },
            { name: 'error(code, message)', type: 'void', description: 'Returns a specific SMTP error code and message.' },
            { name: 'throttle(bps)', type: 'boolean', description: 'Throttles connect speed to specified bits per second. Returns true.' },
            { name: 'disconnect()', type: 'void', description: 'Disconnects the session immediately.' }
        ];
    }
    
    getJavaScriptOperators() {
        return [
            { operator: '==', name: 'Equal', description: 'Compares two values for equality' },
            { operator: '!=', name: 'Not Equal', description: 'Compares two values for inequality' },
            { operator: '===', name: 'Strict Equal', description: 'Compares two values for strict equality (type and value)' },
            { operator: '!==', name: 'Strict Not Equal', description: 'Compares two values for strict inequality' },
            { operator: '<', name: 'Less Than', description: 'Checks if left value is less than right value' },
            { operator: '>', name: 'Greater Than', description: 'Checks if left value is greater than right value' },
            { operator: '<=', name: 'Less/Equal', description: 'Checks if left value is less than or equal to right value' },
            { operator: '>=', name: 'Greater/Equal', description: 'Checks if left value is greater than or equal to right value' },
            { operator: '&&', name: 'Logical AND', description: 'Returns true if both operands are true' },
            { operator: '||', name: 'Logical OR', description: 'Returns true if either operand is true' },
            { operator: '!', name: 'Logical NOT', description: 'Returns the opposite boolean value' },
            { operator: '+', name: 'Addition', description: 'Adds two numbers or concatenates strings' },
            { operator: '-', name: 'Subtraction', description: 'Subtracts the right operand from the left operand' },
            { operator: '*', name: 'Multiplication', description: 'Multiplies two numbers' },
            { operator: '/', name: 'Division', description: 'Divides the left operand by the right operand' },
            { operator: '%', name: 'Modulo', description: 'Returns the remainder of a division operation' },
            { operator: '?:', name: 'Ternary', description: 'Conditional operator: condition ? valueIfTrue : valueIfFalse' }
        ];
    }
    
    getExampleExpressions(): Example[] {
        switch (this.expressionType) {
            case 'credentials':
                return [
                    {
                        title: 'Simple Username/Password Check',
                        code: "credentials.Type == 'USERNAME_PASSWORD' && credentials.username == 'rob' && credentials.password == 'pass'",
                        description: 'Accept only specific username and password combination (from config file example)'
                    }
                ];
                
            case 'recipient':
                return [
                    {
                        title: 'Accept Specific Recipient',
                        code: 'recipient == "foo@bar.com"',
                        description: 'Accepts this recipient only'
                    },
                    {
                        title: 'Reject Specific Recipient',
                        code: 'recipient != "foo@bar.com"',
                        description: 'Rejects this recipient only'
                    }
                ];
                
            case 'message':
                return [
                    {
                        title: 'Subject Content Filter',
                        code: '!message.subject.includes("19")',
                        description: 'Rejects messages that include "19" in the subject'
                    },
                    {
                        title: 'Conditional Response Code',
                        code: 'message.subject.includes("19") ? 441 : null',
                        description: 'Rejects messages that include "19" with a 441 error code, otherwise accepts'
                    }
                ];
                
            case 'command':
                return [
                    {
                        title: 'HELO Command Validation',
                        code: 'command.Verb == "HELO" && command.ArgumentsText == "rob"',
                        description: 'Rejects "HELO rob" command'
                    },
                    {
                        title: 'Custom Error Response',
                        code: 'command.Verb == "HELO" && command.ArgumentsText == "rob" ? error(123, "Rob is not welcome here") : null',
                        description: 'Rejects "HELO rob" with a custom error message'
                    }
                ];
                
            case 'relay':
                return [
                    {
                        title: 'Subject-based Relay',
                        code: "message.subject.includes('QP')",
                        description: 'If message includes QP in the subject then relay to original recipient'
                    },
                    {
                        title: 'Domain Replacement',
                        code: "recipient.replace(/@mailinator.com$/,'@smtp4dev.com')",
                        description: 'Relay all messages to their original recipient except those to @mailinator.com, which are relayed instead to @smtp4dev.com'
                    },
                    {
                        title: 'Conditional Recipient Override',
                        code: "message.subject.includes('QP') ? 'newrecip@test.com' : null",
                        description: 'If message includes QP in the subject then relay to newrecip@test.com, otherwise don\'t relay'
                    },
                    {
                        title: 'Conditional Recipient or Original',
                        code: "message.subject.includes('QP') ? 'newrecip@test.com' : recipient",
                        description: 'If message includes QP in the subject then relay to newrecip@test.com, otherwise relay to original recipient'
                    }
                ];
                
            default:
                return [];
        }
    }
    
    insertAtCursor(text: string) {
        if (this.aceEditorInstance) {
            this.aceEditorInstance.insert(text);
            this.aceEditorInstance.focus();
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
    
    @Emit('update:value')
    handleSave() {
        this.dialogVisible = false;
        return this.editorExpression;
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
    display: flex;
    flex-direction: column;
}

.expression-editor-dialog :deep(.el-dialog__body) {
    flex: 1;
    display: flex;
    flex-direction: column;
    padding: 20px;
}

.expression-editor {
    display: flex;
    flex-direction: column;
    height: 100%;
    min-height: 500px;
}

.editor-header {
    margin-bottom: 20px;
    flex-shrink: 0;
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

.editor-main {
    display: flex;
    flex: 1;
    gap: 20px;
    min-height: 0;
}

.editor-content {
    flex: 1;
    display: flex;
    flex-direction: column;
    min-width: 0;
}

.ace-editor-container {
    flex: 1;
    border: 1px solid #dcdfe6;
    border-radius: 4px;
    overflow: hidden;
    margin-bottom: 16px;
    min-height: 300px;
}

.ace-editor-container :deep(.ace_editor) {
    font-size: 14px !important;
    font-family: 'Monaco', 'Consolas', 'Courier New', monospace !important;
    border-radius: 4px;
    height: 100% !important;
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

.editor-toolbar {
    display: flex;
    justify-content: space-between;
    align-items: center;
    padding: 8px 0;
    border-top: 1px solid #ebeef5;
    flex-shrink: 0;
}

.toolbar-left,
.toolbar-right {
    display: flex;
    gap: 8px;
}

.validation-result {
    display: flex;
    align-items: center;
    gap: 8px;
    margin-top: 12px;
    padding: 8px 12px;
    border-radius: 4px;
    font-size: 14px;
    flex-shrink: 0;
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

.help-panel {
    width: 350px;
    border-left: 1px solid #ebeef5;
    padding-left: 20px;
    flex-shrink: 0;
    display: flex;
    flex-direction: column;
    min-height: 0;
}

.help-content {
    flex: 1;
    overflow-y: auto;
    padding-right: 8px;
}

.help-section {
    margin-bottom: 16px;
}

.section-header {
    display: flex;
    align-items: center;
    padding: 8px 0;
    cursor: pointer;
    gap: 8px;
    border-bottom: 1px solid #ebeef5;
    margin-bottom: 12px;
}

.section-header:hover {
    background: #f8f9fa;
    border-radius: 4px;
    margin-bottom: 8px;
    padding: 8px 12px;
}

.section-header h4 {
    margin: 0;
    color: #303133;
    font-size: 14px;
    font-weight: 600;
}

.section-content {
    margin-bottom: 12px;
}

.variable-section {
    margin-bottom: 12px;
    border: 1px solid #ebeef5;
    border-radius: 4px;
}

.variable-header {
    display: flex;
    align-items: center;
    padding: 8px 12px;
    background: #f8f9fa;
    cursor: pointer;
    border-radius: 4px;
    gap: 8px;
}

.variable-header:hover {
    background: #f0f0f0;
}

.expand-icon {
    transition: transform 0.2s ease;
    color: #909399;
}

.expand-icon.expanded {
    transform: rotate(90deg);
}

.variable-name {
    font-weight: 600;
    color: #303133;
    min-width: 80px;
}

.variable-type {
    font-family: 'Monaco', 'Consolas', 'Courier New', monospace;
    background: #e6f4ff;
    color: #0969da;
    padding: 2px 6px;
    border-radius: 3px;
    font-size: 12px;
    min-width: 60px;
    text-align: center;
}

.variable-description {
    color: #606266;
    font-size: 13px;
    flex: 1;
}

.variable-properties {
    padding: 0 12px 12px 12px;
}

.return-values-info {
    margin-top: 8px;
    padding: 8px 12px;
    background: #f0f9ff;
    border: 1px solid #91d5ff;
    border-radius: 4px;
    font-size: 13px;
    color: #067f8c;
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
    font-size: 13px;
}

.example pre {
    margin: 8px 0;
    padding: 8px;
    background: #fff;
    border: 1px solid #ebeef5;
    border-radius: 4px;
    font-size: 11px;
    overflow-x: auto;
    transition: background-color 0.2s ease;
}

.example pre.clickable-item:hover {
    background: #f0f9ff;
    border-color: #91d5ff;
}

.example p {
    margin: 8px 0 0 0;
    color: #909399;
    font-size: 12px;
}

.dialog-footer {
    display: flex;
    justify-content: flex-end;
    gap: 12px;
}

.clickable-item {
    cursor: pointer;
    color: #409eff;
    text-decoration: underline;
    transition: color 0.2s ease;
}

.clickable-item:hover {
    color: #66b1ff;
    text-decoration: none;
}

.operator-text {
    font-family: 'Monaco', 'Consolas', 'Courier New', monospace;
    font-weight: bold;
    background: #f0f9ff;
    padding: 2px 6px;
    border-radius: 3px;
    border: 1px solid #91d5ff;
}
</style>