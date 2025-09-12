<template>
    <div class="expression-builder">
        <div class="builder-section">
            <h4>Build Your Expression</h4>
            <p class="description">Create conditions and actions using the dropdown options below.</p>
            
            <!-- Quick Templates -->
            <div class="templates-section mb-3">
                <el-select 
                    v-model="selectedTemplate" 
                    placeholder="Choose a template..."
                    @change="applyTemplate"
                    clearable>
                    <el-option 
                        v-for="template in availableTemplates" 
                        :key="template.id"
                        :label="template.name"
                        :value="template.id">
                        <span>{{ template.name }}</span>
                        <span style="color: #8492a6; font-size: 12px; margin-left: 8px;">{{ template.description }}</span>
                    </el-option>
                </el-select>
            </div>
            
            <!-- Condition Builder -->
            <div class="conditions-section">
                <div class="section-header">
                    <h5>Conditions</h5>
                    <el-button size="small" @click="addCondition">Add Condition</el-button>
                </div>
                
                <div v-if="conditions.length === 0" class="empty-state">
                    No conditions. Click "Add Condition" to start building your expression.
                </div>
                
                <div v-for="(condition, index) in conditions" :key="index" class="condition-row">
                    <div class="condition-builder">
                        <!-- Left operand -->
                        <el-select v-model="condition.left" placeholder="Choose variable...">
                            <el-option-group 
                                v-for="group in variableGroups" 
                                :key="group.label"
                                :label="group.label">
                                <el-option 
                                    v-for="variable in group.variables"
                                    :key="variable.value"
                                    :label="variable.label"
                                    :value="variable.value">
                                </el-option>
                            </el-option-group>
                        </el-select>
                        
                        <!-- Operator -->
                        <el-select v-model="condition.operator" placeholder="Choose operator...">
                            <el-option 
                                v-for="op in operators"
                                :key="op.value"
                                :label="op.label"
                                :value="op.value">
                            </el-option>
                        </el-select>
                        
                        <!-- Right operand -->
                        <el-input 
                            v-model="condition.right" 
                            placeholder="Enter value..."
                            class="condition-value">
                        </el-input>
                        
                        <!-- Logical connector for next condition -->
                        <el-select 
                            v-if="index < conditions.length - 1"
                            v-model="condition.connector" 
                            style="width: 80px;">
                            <el-option label="AND" value="&&"></el-option>
                            <el-option label="OR" value="||"></el-option>
                        </el-select>
                        
                        <!-- Remove condition -->
                        <el-button 
                            size="small" 
                            type="danger" 
                            icon="delete"
                            @click="removeCondition(index)">
                        </el-button>
                    </div>
                </div>
            </div>
            
            <!-- Action Builder (for validation expressions) -->
            <div v-if="showActions" class="actions-section mt-3">
                <div class="section-header">
                    <h5>Action</h5>
                </div>
                
                <el-radio-group v-model="actionType">
                    <el-radio label="accept">Accept</el-radio>
                    <el-radio label="reject">Reject</el-radio>
                    <el-radio label="custom">Custom Response</el-radio>
                </el-radio-group>
                
                <div v-if="actionType === 'custom'" class="custom-action mt-2">
                    <el-form-item label="Response Code">
                        <el-input-number v-model="customCode" :min="100" :max="599"></el-input-number>
                    </el-form-item>
                    <el-form-item label="Response Message">
                        <el-input v-model="customMessage" placeholder="Custom error message"></el-input>
                    </el-form-item>
                </div>
            </div>
            
            <!-- Generated Expression Preview -->
            <div class="generated-expression mt-3">
                <h5>Generated Expression:</h5>
                <el-card>
                    <pre><code>{{ generatedExpression || '(empty)' }}</code></pre>
                </el-card>
            </div>
        </div>
    </div>
</template>

<script lang="ts">
import { Component, Vue, Prop, Emit, Watch, toNative } from 'vue-facing-decorator';

interface Condition {
    left: string;
    operator: string;
    right: string;
    connector: string;
}

interface Template {
    id: string;
    name: string;
    description: string;
    expression: string;
    conditions?: Condition[];
}

interface Variable {
    label: string;
    value: string;
}

interface VariableGroup {
    label: string;
    variables: Variable[];
}

@Component
class ExpressionBuilder extends Vue {
    
    @Prop({ default: 'generic' })
    expressionType!: 'credentials' | 'recipient' | 'message' | 'command' | 'relay' | 'generic';
    
    @Prop({ default: null })
    value!: any;
    
    selectedTemplate: string = '';
    conditions: Condition[] = [];
    actionType: 'accept' | 'reject' | 'custom' = 'accept';
    customCode: number = 550;
    customMessage: string = '';
    
    operators = [
        { label: 'equals (==)', value: '==' },
        { label: 'not equals (!=)', value: '!=' },
        { label: 'contains', value: '.includes(' },
        { label: 'does not contain', value: '!.includes(' },
        { label: 'starts with', value: '.startsWith(' },
        { label: 'ends with', value: '.endsWith(' },
        { label: 'matches regex', value: '.match(' },
        { label: 'greater than (>)', value: '>' },
        { label: 'less than (<)', value: '<' },
        { label: 'greater or equal (>=)', value: '>=' },
        { label: 'less or equal (<=)', value: '<=' }
    ];
    
    get variableGroups(): VariableGroup[] {
        const groups: VariableGroup[] = [
            {
                label: 'Session',
                variables: [
                    { label: 'Client Address', value: 'session.clientAddress' },
                    { label: 'Client Name', value: 'session.clientName' },
                    { label: 'Session ID', value: 'session.id' }
                ]
            }
        ];
        
        switch (this.expressionType) {
            case 'credentials':
                groups.push({
                    label: 'Credentials',
                    variables: [
                        { label: 'Type', value: 'credentials.Type' },
                        { label: 'Username', value: 'credentials.username' },
                        { label: 'Password', value: 'credentials.password' }
                    ]
                });
                break;
                
            case 'recipient':
                groups.push({
                    label: 'Recipient',
                    variables: [
                        { label: 'Email Address', value: 'recipient' }
                    ]
                });
                break;
                
            case 'message':
                groups.push({
                    label: 'Message',
                    variables: [
                        { label: 'Subject', value: 'message.subject' },
                        { label: 'From Address', value: 'message.from' },
                        { label: 'Message ID', value: 'message.id' }
                    ]
                });
                break;
                
            case 'command':
                groups.push({
                    label: 'Command',
                    variables: [
                        { label: 'Command Verb', value: 'command.Verb' },
                        { label: 'Arguments', value: 'command.ArgumentsText' }
                    ]
                });
                break;
                
            case 'relay':
                groups.push(
                    {
                        label: 'Recipient',
                        variables: [
                            { label: 'Email Address', value: 'recipient' }
                        ]
                    },
                    {
                        label: 'Message',
                        variables: [
                            { label: 'Subject', value: 'message.subject' },
                            { label: 'From Address', value: 'message.from' }
                        ]
                    }
                );
                break;
        }
        
        return groups;
    }
    
    get availableTemplates(): Template[] {
        const baseTemplates: Template[] = [
            {
                id: 'allow-all',
                name: 'Allow All',
                description: 'Accept all requests',
                expression: 'true'
            },
            {
                id: 'deny-all',
                name: 'Deny All', 
                description: 'Reject all requests',
                expression: 'false'
            }
        ];
        
        switch (this.expressionType) {
            case 'credentials':
                return [
                    ...baseTemplates,
                    {
                        id: 'username-password',
                        name: 'Username/Password Check',
                        description: 'Basic username and password validation',
                        expression: "credentials.Type == 'USERNAME_PASSWORD' && credentials.username == 'admin' && credentials.password == 'password'",
                        conditions: [
                            { left: 'credentials.Type', operator: '==', right: "'USERNAME_PASSWORD'", connector: '&&' },
                            { left: 'credentials.username', operator: '==', right: "'admin'", connector: '&&' },
                            { left: 'credentials.password', operator: '==', right: "'password'", connector: '' }
                        ]
                    }
                ];
                
            case 'recipient':
                return [
                    ...baseTemplates,
                    {
                        id: 'domain-check',
                        name: 'Domain Whitelist',
                        description: 'Only allow specific domains',
                        expression: "recipient.endsWith('@example.com')",
                        conditions: [
                            { left: 'recipient', operator: '.endsWith(', right: "'@example.com')", connector: '' }
                        ]
                    },
                    {
                        id: 'block-domain',
                        name: 'Block Domain',
                        description: 'Block specific domains',
                        expression: "!recipient.endsWith('@spam.com')",
                        conditions: [
                            { left: 'recipient', operator: '!.endsWith(', right: "'@spam.com')", connector: '' }
                        ]
                    }
                ];
                
            case 'message':
                return [
                    ...baseTemplates,
                    {
                        id: 'subject-filter',
                        name: 'Subject Filter',
                        description: 'Filter by subject content',
                        expression: "!message.subject.includes('SPAM')",
                        conditions: [
                            { left: 'message.subject', operator: '!.includes(', right: "'SPAM')", connector: '' }
                        ]
                    }
                ];
                
            case 'command':
                return [
                    ...baseTemplates,
                    {
                        id: 'helo-check',
                        name: 'HELO Command Check',
                        description: 'Validate HELO commands',
                        expression: "command.Verb == 'HELO' && command.ArgumentsText.length > 0",
                        conditions: [
                            { left: 'command.Verb', operator: '==', right: "'HELO'", connector: '&&' },
                            { left: 'command.ArgumentsText.length', operator: '>', right: '0', connector: '' }
                        ]
                    }
                ];
                
            case 'relay':
                return [
                    ...baseTemplates,
                    {
                        id: 'relay-domain',
                        name: 'Relay to Different Domain',
                        description: 'Relay to a different domain',
                        expression: "recipient.replace(/@example.com$/, '@real-domain.com')",
                        conditions: []
                    }
                ];
                
            default:
                return baseTemplates;
        }
    }
    
    get showActions(): boolean {
        return ['credentials', 'recipient', 'message', 'command'].includes(this.expressionType);
    }
    
    get generatedExpression(): string {
        if (this.conditions.length === 0) {
            return '';
        }
        
        let expression = '';
        
        for (let i = 0; i < this.conditions.length; i++) {
            const condition = this.conditions[i];
            
            if (!condition.left || !condition.operator) {
                continue;
            }
            
            let conditionStr = '';
            
            // Handle special operators
            if (condition.operator.includes('.includes(')) {
                const isNegated = condition.operator.startsWith('!');
                const right = condition.right.startsWith("'") ? condition.right : `'${condition.right}'`;
                conditionStr = `${isNegated ? '!' : ''}${condition.left}.includes(${right})`;
            } else if (condition.operator.includes('.startsWith(')) {
                const right = condition.right.startsWith("'") ? condition.right : `'${condition.right}'`;
                conditionStr = `${condition.left}.startsWith(${right})`;
            } else if (condition.operator.includes('.endsWith(')) {
                const isNegated = condition.operator.startsWith('!');
                const right = condition.right.startsWith("'") ? condition.right : `'${condition.right}'`;
                conditionStr = `${isNegated ? '!' : ''}${condition.left}.endsWith(${right})`;
            } else if (condition.operator.includes('.match(')) {
                const right = condition.right.startsWith('/') ? condition.right : `/${condition.right}/`;
                conditionStr = `${condition.left}.match(${right})`;
            } else {
                // Simple operators
                const right = (condition.operator === '==' || condition.operator === '!=') && 
                             !condition.right.startsWith("'") && !condition.right.match(/^\d+$/) 
                             ? `'${condition.right}'` : condition.right;
                conditionStr = `${condition.left} ${condition.operator} ${right}`;
            }
            
            if (expression) {
                expression += ` ${condition.connector} `;
            }
            
            expression += conditionStr;
        }
        
        // Add action for validation expressions
        if (this.showActions && expression) {
            if (this.actionType === 'reject') {
                expression = `!(${expression})`;
            } else if (this.actionType === 'custom') {
                expression = `(${expression}) ? true : error(${this.customCode}, '${this.customMessage}')`;
            }
        }
        
        return expression;
    }
    
    addCondition() {
        this.conditions.push({
            left: '',
            operator: '',
            right: '',
            connector: '&&'
        });
    }
    
    removeCondition(index: number) {
        this.conditions.splice(index, 1);
    }
    
    applyTemplate() {
        const template = this.availableTemplates.find(t => t.id === this.selectedTemplate);
        if (template) {
            if (template.conditions) {
                this.conditions = [...template.conditions];
            } else {
                // For simple expressions, try to parse them
                this.conditions = [];
            }
            this.emitValue();
        }
    }
    
    @Watch('conditions', { deep: true })
    onConditionsChanged() {
        this.emitValue();
    }
    
    @Watch(['actionType', 'customCode', 'customMessage'])
    onActionChanged() {
        this.emitValue();
    }
    
    @Emit('update:value')
    emitValue() {
        return {
            expression: this.generatedExpression,
            conditions: this.conditions,
            actionType: this.actionType,
            customCode: this.customCode,
            customMessage: this.customMessage
        };
    }
    
    mounted() {
        // Initialize with a default condition if empty
        if (this.conditions.length === 0) {
            this.addCondition();
        }
    }
}

export default toNative(ExpressionBuilder);
</script>

<style scoped>
.expression-builder {
    min-height: 300px;
}

.builder-section {
    padding: 10px;
}

.description {
    color: #666;
    margin-bottom: 16px;
}

.section-header {
    display: flex;
    justify-content: space-between;
    align-items: center;
    margin-bottom: 12px;
}

.section-header h5 {
    margin: 0;
}

.empty-state {
    text-align: center;
    color: #999;
    padding: 20px;
    border: 2px dashed #ddd;
    border-radius: 4px;
}

.condition-row {
    margin-bottom: 12px;
}

.condition-builder {
    display: flex;
    gap: 8px;
    align-items: center;
    flex-wrap: wrap;
}

.condition-builder .el-select,
.condition-builder .el-input {
    min-width: 120px;
}

.condition-value {
    flex: 1;
    min-width: 150px;
}

.generated-expression {
    border-top: 1px solid #eee;
    padding-top: 16px;
}

.generated-expression pre {
    margin: 0;
    white-space: pre-wrap;
    word-break: break-word;
}

.custom-action {
    background: #f9f9f9;
    padding: 12px;
    border-radius: 4px;
}

.mb-3 {
    margin-bottom: 16px;
}

.mt-2 {
    margin-top: 8px;
}

.mt-3 {
    margin-top: 16px;
}

.templates-section {
    border-bottom: 1px solid #eee;
    padding-bottom: 16px;
}
</style>