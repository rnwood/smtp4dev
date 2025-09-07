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
            this.emailSupportsDarkMode = false; // Reset first

            if (this.html) {
                // Check if email supports dark mode before sanitization
                const originalDarkModeSupport = this.detectDarkModeSupport(this.html);
                console.log('Dark mode detection on original HTML:', originalDarkModeSupport, 'for HTML length:', this.html.length);

                if (!this.enableSanitization) {
                    this.sanitizedHtml = this.html;
                    this.emailSupportsDarkMode = originalDarkModeSupport;
                } else {
                    // Allow additional tags and attributes needed for dark mode detection
                    const sanitizeOptions = {
                        allowedTags: sanitizeHtml.defaults.allowedTags.concat([
                            "img", "style", "meta", "head", "html", "body"
                        ]),
                        allowedAttributes: {
                            ...sanitizeHtml.defaults.allowedAttributes,
                            "meta": ["name", "content", "charset", "http-equiv"],
                            "html": ["lang", "dir"],
                            "body": ["class"],
                            "*": ["style", "class", "id"] // Allow style, class, and id on all elements for better CSS support
                        },
                        allowedSchemesByTag: { 
                            "img": ["cid", "data"] 
                        }
                    };
                    
                    this.sanitizedHtml = sanitizeHtml(this.html, sanitizeOptions);
                    let normalizedOriginalHtml = sanitizeHtml(this.html, { allowedAttributes: false, allowedTags: false, allowVulnerableTags: true });
                    this.wasSanitized = normalizedOriginalHtml !== this.sanitizedHtml;
                    
                    // Check dark mode support on sanitized HTML
                    this.emailSupportsDarkMode = this.detectDarkModeSupport(this.sanitizedHtml);
                    console.log('Dark mode detection on sanitized HTML:', this.emailSupportsDarkMode, 'for sanitized HTML length:', this.sanitizedHtml.length);
                    
                    if (originalDarkModeSupport !== this.emailSupportsDarkMode) {
                        console.warn('⚠️ Dark mode detection result changed after sanitization!', 
                                   'Original:', originalDarkModeSupport, 'Sanitized:', this.emailSupportsDarkMode);
                    }
                }
            }

            srcDoc.set(this.$refs.htmlframe as HTMLIFrameElement, this.sanitizedHtml);
        }

        private detectDarkModeSupport(html: string): boolean {
            try {
                console.log('Detecting dark mode support in HTML...');
                
                // Use DOMParser to safely parse HTML
                const parser = new DOMParser();
                const doc = parser.parseFromString(html, 'text/html');

                // Check for supported-color-schemes meta tag
                const supportedColorSchemesMeta = doc.querySelector('meta[name="supported-color-schemes"]');
                if (supportedColorSchemesMeta) {
                    const content = supportedColorSchemesMeta.getAttribute('content') || '';
                    console.log('Found supported-color-schemes meta tag with content:', content);
                    if (this.parseColorSchemeValues(content).includes('dark')) {
                        console.log('✅ Dark mode detected via supported-color-schemes meta tag');
                        return true;
                    }
                }

                // Check for color-scheme meta tag
                const colorSchemeMeta = doc.querySelector('meta[name="color-scheme"]');
                if (colorSchemeMeta) {
                    const content = colorSchemeMeta.getAttribute('content') || '';
                    console.log('Found color-scheme meta tag with content:', content);
                    if (this.parseColorSchemeValues(content).includes('dark')) {
                        console.log('✅ Dark mode detected via color-scheme meta tag');
                        return true;
                    }
                }

                // Check for CSS media queries that indicate dark mode support
                const styleElements = doc.querySelectorAll('style');
                console.log('Found', styleElements.length, 'style elements');
                for (const styleElement of styleElements) {
                    const cssText = styleElement.textContent || '';
                    if (cssText && this.parseCSSForDarkModeQueries(cssText)) {
                        console.log('✅ Dark mode detected via CSS media query');
                        return true;
                    }
                }

                // Also check for dark mode media queries in linked stylesheets or inline styles
                // Note: We can't access external stylesheets due to CORS, but we can check style attributes
                const elementsWithStyle = doc.querySelectorAll('[style]');
                for (const element of elementsWithStyle) {
                    const styleAttr = element.getAttribute('style') || '';
                    if (this.parseCSSForDarkModeQueries(styleAttr)) {
                        console.log('✅ Dark mode detected via inline style');
                        return true;
                    }
                }

                console.log('❌ No dark mode support detected');
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

            // First try simple string search as it's more reliable for this use case
            const simpleCheck = cssText.toLowerCase().includes('prefers-color-scheme') && cssText.toLowerCase().includes('dark');
            if (simpleCheck) {
                console.log('✅ Found dark mode media query via simple string search');
                return true;
            }

            try {
                // Try css-tree parsing as a secondary check
                const ast = csstree.parse(cssText, { parseRulePrelude: false });
                
                let foundDarkModeQuery = false;
                
                csstree.walk(ast, function(node) {
                    if (node.type === 'Atrule' && node.name === 'media') {
                        const mediaQueryText = csstree.generate(node.prelude);
                        console.log('Found @media rule:', mediaQueryText);
                        if (mediaQueryText.includes('prefers-color-scheme') && mediaQueryText.includes('dark')) {
                            console.log('✅ Found dark mode media query via css-tree:', mediaQueryText);
                            foundDarkModeQuery = true;
                        }
                    }
                });

                return foundDarkModeQuery;
            } catch (error) {
                console.warn('Error parsing CSS with css-tree, using fallback:', error);
                return simpleCheck;
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

        private serverChangedHandler: (() => void) | null = null;

        @Watch("connection")
        async onConnectionChanged() {
            if (this.connection) {
                this.enableSanitization = !(await this.connection.getServer()).disableMessageSanitisation;
                
                // Remove any existing handler first to avoid duplicates
                if (this.serverChangedHandler) {
                    // Note: We don't have a way to remove handlers yet, but we prevent duplicates
                    // by only setting the handler once
                }
                
                // Create new handler
                this.serverChangedHandler = async () => {
                    const newSetting = !(await this.connection!.getServer()).disableMessageSanitisation;
                    this.enableSanitization = newSetting;
                    this.updateIframe();
                };
                
                this.connection.onServerChanged(this.serverChangedHandler);
                
                // Re-process HTML with the correct sanitization setting
                this.updateIframe();
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
            // Initialize sanitization setting if connection is already available
            if (this.connection) {
                this.enableSanitization = !(await this.connection.getServer()).disableMessageSanitisation;
                // Since connection is already available, we need to manually call onConnectionChanged
                // to register the server change callback
                await this.onConnectionChanged();
            }
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