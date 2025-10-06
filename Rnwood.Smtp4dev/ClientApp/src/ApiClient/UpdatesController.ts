import axios from "axios";
import UpdateCheckResult from "./UpdateCheckResult";

export default class UpdatesController {
    constructor() {}

    public checkForUpdates_url(username: string | null = null): string {
        let url = "api/Updates/check";
        if (username) {
            url += `?username=${encodeURIComponent(username)}`;
        }
        return url;
    }

    public async checkForUpdates(username: string | null = null): Promise<UpdateCheckResult> {
        return (await axios.get(this.checkForUpdates_url(username))).data as UpdateCheckResult;
    }

    public markVersionAsSeen_url(username: string, version: string): string {
        return `api/Updates/mark-seen?username=${encodeURIComponent(username)}&version=${encodeURIComponent(version)}`;
    }

    public async markVersionAsSeen(username: string, version: string): Promise<void> {
        await axios.post(this.markVersionAsSeen_url(username, version));
    }
}
