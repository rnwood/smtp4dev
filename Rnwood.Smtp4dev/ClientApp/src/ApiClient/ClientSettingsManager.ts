import ClientSettings from "./ClientSettings";
import ClientSettingsController from "./ClientSettingsController";

export interface ClientSettingsData {
    pageSize: number;
    autoViewNewMessages: boolean;
}

type SettingsChangeCallback = (oldSettings: ClientSettingsData | null, newSettings: ClientSettingsData) => void;

export default class ClientSettingsManager {
    private static readonly STORAGE_KEY = "smtp4dev-client-settings";
    private static settingsChangeListeners: Set<SettingsChangeCallback> = new Set();

    /**
     * Emit settings change event to all registered listeners
     */
    private static emitSettingsChange(oldSettings: ClientSettingsData | null, newSettings: ClientSettingsData): void {
        this.settingsChangeListeners.forEach(callback => {
            try {
                callback(oldSettings, newSettings);
            } catch (error) {
                console.error('Error in settings change listener:', error);
            }
        });
    }

    /**
     * Get client settings, merging server defaults with locally stored user preferences
     */
    public static async getClientSettings(): Promise<ClientSettings> {
        // Get server defaults
        const serverSettings = await new ClientSettingsController().getClientSettings();

        // Get locally stored preferences
        const storedSettings = this.getStoredSettings();

        // Merge: local preferences override server defaults
        return new ClientSettings(
            storedSettings?.pageSize ?? serverSettings.pageSize,
            storedSettings?.autoViewNewMessages ?? serverSettings.autoViewNewMessages
        );
    }

    /**
     * Save client settings to local storage
     */
    public static saveClientSettings(settings: ClientSettings): void {
        const oldSettings = this.getStoredSettings();
        const settingsToStore: ClientSettingsData = {
            pageSize: settings.pageSize,
            autoViewNewMessages: settings.autoViewNewMessages
        };
        localStorage.setItem(this.STORAGE_KEY, JSON.stringify(settingsToStore));
        this.emitSettingsChange(oldSettings, settingsToStore);
    }

    /**
     * Update specific client settings and save to local storage
     */
    public static updateClientSettings(updates: Partial<ClientSettingsData>): void {
        const oldSettings = this.getStoredSettings();
        const currentSettings = oldSettings || { pageSize: 30, autoViewNewMessages: false };
        const updatedSettings: ClientSettingsData = { ...currentSettings, ...updates };
        localStorage.setItem(this.STORAGE_KEY, JSON.stringify(updatedSettings));
        this.emitSettingsChange(oldSettings, updatedSettings);
    }

    /**
     * Get locally stored settings (public accessor)
     */
    public static getStoredSettings(): ClientSettingsData | null {
        try {
            const stored = localStorage.getItem(this.STORAGE_KEY);
            return stored ? JSON.parse(stored) : null;
        } catch (e) {
            console.warn("Failed to parse stored client settings:", e);
            return null;
        }
    }

    /**
     * Clear locally stored settings (reset to server defaults)
     */
    public static clearStoredSettings(): void {
        localStorage.removeItem(this.STORAGE_KEY);
    }

    /**
     * Listen for settings changes
     */
    public static onSettingsChanged(callback: SettingsChangeCallback): () => void {
        this.settingsChangeListeners.add(callback);

        // Return unsubscribe function
        return () => {
            this.settingsChangeListeners.delete(callback);
        };
    }
}