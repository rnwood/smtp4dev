<template>
    <el-dialog 
        title="What's New" 
        :visible="visible" 
        width="70%" 
        :close-on-click-modal="false"
        @close="handleClose">
        
        <div v-loading="loading">
            <el-alert v-if="error" type="error" :title="error" show-icon closable />

            <div v-if="updateCheckResult">
                <el-alert 
                    v-if="updateCheckResult.updateAvailable"
                    type="warning" 
                    title="Update Available" 
                    :closable="false"
                    show-icon>
                    A new version is available!
                </el-alert>

                <div class="version-info">
                    <p><strong>Current Version:</strong> {{ updateCheckResult.currentVersion }}</p>
                    <p v-if="updateCheckResult.lastSeenVersion">
                        <strong>Last Seen Version:</strong> {{ updateCheckResult.lastSeenVersion }}
                    </p>
                </div>

                <div v-if="updateCheckResult.newReleases && updateCheckResult.newReleases.length > 0" class="releases">
                    <h3>Release Notes</h3>
                    <el-timeline>
                        <el-timeline-item 
                            v-for="release in updateCheckResult.newReleases" 
                            :key="release.tagName"
                            :timestamp="formatDate(release.publishedAt)"
                            placement="top">
                            <el-card>
                                <div class="release-header">
                                    <h4>
                                        {{ release.name || release.tagName }}
                                        <el-tag v-if="release.prerelease" size="small" type="warning">Prerelease</el-tag>
                                    </h4>
                                </div>
                                <div class="release-body" v-html="formatMarkdown(release.body)"></div>
                                <div class="release-footer">
                                    <el-link :href="release.htmlUrl" target="_blank" type="primary">
                                        View on GitHub
                                    </el-link>
                                </div>
                            </el-card>
                        </el-timeline-item>
                    </el-timeline>
                </div>

                <div v-else class="no-releases">
                    <p>No new releases found.</p>
                </div>
            </div>
        </div>

        <template #footer>
            <div class="dialog-footer">
                <el-button @click="handleDismiss">Dismiss</el-button>
                <el-button type="primary" @click="handleMarkAsRead">Mark as Read</el-button>
            </div>
        </template>
    </el-dialog>
</template>

<script lang="ts">
import { Component, Prop, Vue } from 'vue-facing-decorator';
import UpdateCheckResult from '@/ApiClient/UpdateCheckResult';
import UpdateNotificationManager from '@/UpdateNotificationManager';

@Component
export default class WhatsNewDialog extends Vue {
    @Prop() visible!: boolean;
    @Prop() updateCheckResult!: UpdateCheckResult | null;

    loading = false;
    error: string | null = null;
    updateManager = new UpdateNotificationManager();

    handleClose() {
        this.$emit('close');
    }

    handleDismiss() {
        this.$emit('dismiss');
        this.handleClose();
    }

    async handleMarkAsRead() {
        if (!this.updateCheckResult) return;

        this.loading = true;
        this.error = null;

        try {
            await this.updateManager.markVersionAsSeen(this.updateCheckResult.currentVersion);
            this.$emit('marked-read');
            this.handleClose();
        } catch (err) {
            this.error = 'Failed to mark as read: ' + (err as Error).message;
        } finally {
            this.loading = false;
        }
    }

    formatDate(dateStr: string): string {
        if (!dateStr) return '';
        const date = new Date(dateStr);
        return date.toLocaleDateString(undefined, { 
            year: 'numeric', 
            month: 'long', 
            day: 'numeric' 
        });
    }

    formatMarkdown(text: string): string {
        if (!text) return '';
        
        // Simple markdown formatting
        let html = text
            // Convert headers
            .replace(/^### (.*$)/gim, '<h5>$1</h5>')
            .replace(/^## (.*$)/gim, '<h4>$1</h4>')
            .replace(/^# (.*$)/gim, '<h3>$1</h3>')
            // Convert bold
            .replace(/\*\*(.*?)\*\*/gim, '<strong>$1</strong>')
            // Convert links
            .replace(/\[([^\]]+)\]\(([^)]+)\)/gim, '<a href="$2" target="_blank">$1</a>')
            // Convert line breaks
            .replace(/\n/gim, '<br>');

        return html;
    }
}
</script>

<style scoped>
.version-info {
    margin-bottom: 20px;
    padding: 10px;
    background-color: var(--el-fill-color-light);
    border-radius: 4px;
}

.releases {
    margin-top: 20px;
}

.release-header h4 {
    margin: 0;
    display: inline-block;
    margin-right: 10px;
}

.release-body {
    margin: 15px 0;
    line-height: 1.6;
}

.release-body :deep(h3),
.release-body :deep(h4),
.release-body :deep(h5) {
    margin-top: 10px;
    margin-bottom: 5px;
}

.release-body :deep(a) {
    color: var(--el-color-primary);
    text-decoration: none;
}

.release-body :deep(a:hover) {
    text-decoration: underline;
}

.release-footer {
    margin-top: 10px;
    text-align: right;
}

.no-releases {
    text-align: center;
    padding: 40px;
    color: var(--el-text-color-secondary);
}

.dialog-footer {
    display: flex;
    justify-content: flex-end;
    gap: 10px;
}
</style>
