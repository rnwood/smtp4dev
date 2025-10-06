import axios from "axios";

export default class ServerLogController {
    constructor() {}

    // get: api/ServerLog
    public getServerLog_url(): string {
        return `api/ServerLog`;
    }

    public async getServerLog(): Promise<string> {
        return (await axios.get(this.getServerLog_url(), null || undefined))
            .data as string;
    }

    // delete: api/ServerLog
    public clearServerLog_url(): string {
        return `api/ServerLog`;
    }

    public async clearServerLog(): Promise<void> {
        return (await axios.delete(this.clearServerLog_url(), null || undefined))
            .data as void;
    }
}
