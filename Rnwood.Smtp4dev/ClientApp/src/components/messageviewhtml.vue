<template>

    <div class="hfillpanel" v-loading="!html">

        <div class="toolbar">
            <el-select style="flex: 1 0 200px;" v-model="selectedViewportSizeName" filterable @change="viewportRotate=false">
                <el-option v-for="item in availableViewportSizes"
                           :key="item.name"
                           :label="item.name"
                           :value="item.name" />

                <template #prefix>
                    <el-icon><Monitor /></el-icon>
                </template>

            </el-select>

            <el-switch v-model="viewportRotate" v-if="!selectedViewportSize.fill" active-text="Rotate" size="medium" />

            <el-select style="flex: 0 0 100px;" v-model="selectedZoomLevelName">
                <el-option v-for="item in availableZoomLevels"
                           :key="item.name"
                           :label="item.name"
                           :value="item.name" />

                <template #prefix>
                    <el-icon><ZoomOut /></el-icon>
                </template>

            </el-select>

        </div>

        <el-alert v-if="error" type="error">
            {{error.message}}

            <el-button v-on:click="refresh">Retry</el-button>
        </el-alert>
        <el-alert v-if="wasSanitized" type="warning">
            Message HTML was sanitized for display. <el-button type="danger" size="small" v-on:click="disableSanitization">Disable (DANGER!)</el-button>
        </el-alert>

        <div class="fill" style="display: flex; flex-direction: column;">
            <iframe :class="htmlFrameClasses" :style="htmlFrameStyles" @load="onHtmlFrameLoaded" ref="htmlframe"></iframe>
        </div>
    </div>
</template>
<script lang="ts">
    import { Component, Vue, Prop, Watch, toNative } from 'vue-facing-decorator'

    import MessagesController from "../ApiClient/MessagesController";
    import ServerController from "../ApiClient/ServerController";
    import Message from "../ApiClient/Message";
    import * as srcDoc from 'srcdoc-polyfill';
    import sanitizeHtml from 'sanitize-html';
    import { deviceSizes, Brand } from 'device-sizes'
    import * as csstree from 'css-tree';

    type ViewPortSize = {
        name: string
        fill?: boolean
        height?: number,
        width?: number
    }


    type ZoomLevel = {
        name: string,
        scale: number
    }

    @Component
    class MessageViewHtml extends Vue {
        @Prop({ default: null })
        connection: HubConnectionManager | null = null;

        @Prop({ default: null })
        message: Message | null | undefined;
        html: string | null = null;
        enableSanitization = true;
        sanitizedHtml: string | null = null;
        wasSanitized: boolean = false;
        emailSupportsDarkMode: boolean = false;

        availableViewportSizes: ViewPortSize[] = [{ name: "Normal", fill: true }].concat(Object.values(deviceSizes).map(d => ({
            name: `${Brand[d.brand]} ${d.name} (${d.size}")`, fill: false, width: d.width / d.scale, height: d.height / d.scale
        })));
        selectedViewportSizeName = "Normal";
        viewportRotate = false;

        availableZoomLevels: ZoomLevel[] = [{ name: "150%", scale: 1.5 },{ name: "125%", scale: 1.25 },{ name: "100%", scale: 1 }, { name: "75%", scale: 0.75 }, { name: "50%", scale: 0.5 }]
        selectedZoomLevelName = "100%";

        get selectedViewportSize(): ViewPortSize {
            return this.availableViewportSizes.find(s => s.name == this.selectedViewportSizeName) ?? this.availableViewportSizes[0];
        }

        get selectedZoomLevel(): ZoomLevel {
            return this.availableZoomLevels.find(s => s.name == this.selectedZoomLevelName) ?? this.availableZoomLevels[0];
        }

        get htmlFrameStyles() {
            const height = this.viewportRotate ? this.selectedViewportSize.width : this.selectedViewportSize.height;
            const width = this.viewportRotate ? this.selectedViewportSize.height : this.selectedViewportSize.width;

            return {
                border: (this.selectedViewportSize.fill ? '0' : '5px solid black'),
                'align-self': (this.selectedViewportSize.fill ? 'stretch' : 'center'),
                'flex-grow': `${this.selectedViewportSize.fill ? 1 : 0}`,
                'flex-shrink': 0,
                'height': `${(height ?? 0) * this.selectedZoomLevel.scale}px`,
                width: (this.selectedViewportSize.fill ? '100%' : ((width ?? 0) * this.selectedZoomLevel.scale) + 'px'),
                'transform-origin': "top left",
                transform: `scale(${this.selectedZoomLevel.scale})`
            };
        }

        get htmlFrameClasses() {
            return {
                'htmlview': true,
                'supports-dark-mode': this.emailSupportsDarkMode
            };
        }


        error: Error | null = null;
        loading = false;


        @Watch("message")
        async onMessageChanged(value: Message | null, oldValue: Message | null) {

            this.html = "";
            await this.refresh();

        }

        @Watch("html")
        async onHtmlChanged(value: string) {
            this.updateIframe();
        }

        private updateIframe() {
            this.wasSanitized = false;
            this.sanitizedHtml = "";

            if (this.html) {
                // Check if email supports dark mode before sanitization
                this.emailSupportsDarkMode = this.detectDarkModeSupport(this.html);

                if (!this.enableSanitization) {
                    this.sanitizedHtml = this.html;
                } else {
                    this.sanitizedHtml = sanitizeHtml(this.html, { allowedTags: sanitizeHtml.defaults.allowedTags.concat("img"), allowedSchemesByTag: { "img": ["cid", "data"] } });
                    let normalizedOriginalHtml = sanitizeHtml(this.html, { allowedAttributes: false, allowedTags: false, allowVulnerableTags: true });
                    this.wasSanitized = normalizedOriginalHtml !== this.sanitizedHtml;
                }
            } else {
                this.emailSupportsDarkMode = false;
            }

            srcDoc.set(this.$refs.htmlframe as HTMLIFrameElement, this.sanitizedHtml);
        }

        private detectDarkModeSupport(html: string): boolean {
            try {
                // Use DOMParser to safely parse HTML
                const parser = new DOMParser();
                const doc = parser.parseFromString(html, 'text/html');

                // Check for supported-color-schemes meta tag
                const supportedColorSchemesMeta = doc.querySelector('meta[name="supported-color-schemes"]');
                if (supportedColorSchemesMeta) {
                    const content = supportedColorSchemesMeta.getAttribute('content') || '';
                    if (this.parseColorSchemeValues(content).includes('dark')) {
                        return true;
                    }
                }

                // Check for color-scheme meta tag
                const colorSchemeMeta = doc.querySelector('meta[name="color-scheme"]');
                if (colorSchemeMeta) {
                    const content = colorSchemeMeta.getAttribute('content') || '';
                    if (this.parseColorSchemeValues(content).includes('dark')) {
                        return true;
                    }
                }

                // Check for CSS media queries that indicate dark mode support
                const styleElements = doc.querySelectorAll('style');
                for (const styleElement of styleElements) {
                    const cssText = styleElement.textContent || '';
                    if (this.parseCSSForDarkModeQueries(cssText)) {
                        return true;
                    }
                }

                // Also check for dark mode media queries in linked stylesheets or inline styles
                // Note: We can't access external stylesheets due to CORS, but we can check style attributes
                const elementsWithStyle = doc.querySelectorAll('[style]');
                for (const element of elementsWithStyle) {
                    const styleAttr = element.getAttribute('style') || '';
                    if (this.parseCSSForDarkModeQueries(styleAttr)) {
                        return true;
                    }
                }

                return false;
            } catch (error) {
                console.warn('Error parsing HTML for dark mode detection:', error);
                return false;
            }
        }

        private parseColorSchemeValues(content: string): string[] {
            if (!content) return [];
            
            // Split by both spaces and commas, trim each value, and filter out empty values
            return content
                .split(/[\s,]+/)
                .map(value => value.trim().toLowerCase())
                .filter(value => value.length > 0);
        }

        private parseCSSForDarkModeQueries(cssText: string): boolean {
            if (!cssText) return false;

            try {
                // Parse CSS using css-tree
                const ast = csstree.parse(cssText, { parseRulePrelude: false });
                
                // Walk through the AST to find @media rules
                let foundDarkModeQuery = false;
                
                csstree.walk(ast, function(node) {
                    if (node.type === 'Atrule' && node.name === 'media') {
                        // Convert the prelude to string and check for dark mode
                        const mediaQueryText = csstree.generate(node.prelude);
                        if (mediaQueryText.includes('prefers-color-scheme') && mediaQueryText.includes('dark')) {
                            foundDarkModeQuery = true;
                        }
                    }
                });

                return foundDarkModeQuery;
            } catch (error) {
                console.warn('Error parsing CSS for dark mode queries:', error);
                // Fallback to simple string search if css-tree parsing fails
                return cssText.toLowerCase().includes('prefers-color-scheme') && cssText.toLowerCase().includes('dark');
            }
        }

        private checkMediaQueryForDarkMode(mediaQuery: any): boolean {
            // This method is no longer used, keeping for compatibility
            return false;
        }

        async onHtmlFrameLoaded() {
            var doc = (this.$refs.htmlframe as HTMLIFrameElement).contentDocument;
            if (!doc) {
                return;
            }

            var baseElement = doc.body.querySelector("base") || doc.createElement("base");
            baseElement.setAttribute("target", "_blank");

            doc.body.appendChild(baseElement);
        }

        @Watch("connection")
        async onConnectionChanged() {
            if (this.connection) {
                this.enableSanitization = !(await this.connection.getServer()).disableMessageSanitisation;
                this.connection.onServerChanged(async () => {
                    this.enableSanitization = !(await this.connection.getServer()).disableMessageSanitisation;
                    this.updateIframe();
                });
            }
        }

        async refresh() {

            this.error = null;
            this.loading = true;
            this.html = null;
            this.wasSanitized = false;


            try {
                if (this.message != null) {

                    this.html = await new MessagesController().getMessageHtml(this.message.id);
                }
            } catch (e: any) {
                this.error = e;
            } finally {
                this.loading = false;
            }
        }

        async created() {
            this.refresh();
        }

        async destroyed() {

        }

        disableSanitization() {
            this.enableSanitization = false;
            this.updateIframe();
        }
    }

    export default toNative(MessageViewHtml)
</script>