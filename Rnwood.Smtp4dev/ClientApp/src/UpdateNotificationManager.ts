import UpdatesController from "./ApiClient/UpdatesController";
import UpdateCheckResult from "./ApiClient/UpdateCheckResult";

export default class UpdateNotificationManager {
    private updatesController: UpdatesController;
    private lastCheckDate: Date | null = null;
    private checkIntervalMs = 24 * 60 * 60 * 1000; // 24 hours
    private intervalId: any = null;
    private onUpdateAvailableCallback: ((result: UpdateCheckResult) => void) | null = null;
    private onWhatsNewCallback: ((result: UpdateCheckResult) => void) | null = null;

    constructor() {
        this.updatesController = new UpdatesController();
    }

    public async checkForUpdates(): Promise<UpdateCheckResult> {
        const username = this.getUsername();
        const result = await this.updatesController.checkForUpdates(username);
        this.lastCheckDate = new Date();
        this.saveLastCheckDate();

        if (result.updateAvailable && this.onUpdateAvailableCallback) {
            this.onUpdateAvailableCallback(result);
        }

        if (result.showWhatsNew && this.onWhatsNewCallback) {
            this.onWhatsNewCallback(result);
        }

        return result;
    }

    public async markVersionAsSeen(version: string): Promise<void> {
        const username = this.getUsername();
        await this.updatesController.markVersionAsSeen(username, version);
        this.saveSeenVersion(version);
    }

    public onUpdateAvailable(callback: (result: UpdateCheckResult) => void): void {
        this.onUpdateAvailableCallback = callback;
    }

    public onWhatsNew(callback: (result: UpdateCheckResult) => void): void {
        this.onWhatsNewCallback = callback;
    }

    public startPeriodicCheck(): void {
        // Check immediately if we haven't checked today
        const lastCheck = this.getLastCheckDate();
        if (!lastCheck || this.isDayOld(lastCheck)) {
            this.checkForUpdates();
        }

        // Set up periodic checks
        if (this.intervalId) {
            clearInterval(this.intervalId);
        }

        this.intervalId = setInterval(() => {
            this.checkForUpdates();
        }, this.checkIntervalMs);
    }

    public stopPeriodicCheck(): void {
        if (this.intervalId) {
            clearInterval(this.intervalId);
            this.intervalId = null;
        }
    }

    private getUsername(): string {
        // Try to get username from storage or use anonymous
        return localStorage.getItem('smtp4dev_username') || 'anonymous';
    }

    private getLastCheckDate(): Date | null {
        const stored = localStorage.getItem('smtp4dev_last_update_check');
        if (stored) {
            return new Date(stored);
        }
        return null;
    }

    private saveLastCheckDate(): void {
        localStorage.setItem('smtp4dev_last_update_check', new Date().toISOString());
    }

    private isDayOld(date: Date): boolean {
        const now = new Date();
        const diffMs = now.getTime() - date.getTime();
        return diffMs >= this.checkIntervalMs;
    }

    private saveSeenVersion(version: string): void {
        localStorage.setItem('smtp4dev_last_seen_version', version);
    }

    public getSeenVersion(): string | null {
        return localStorage.getItem('smtp4dev_last_seen_version');
    }
}
