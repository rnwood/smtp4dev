import axios from "axios";
import LogEntry from "./LogEntry";

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

    // get: api/ServerLog/entries
    public getServerLogEntries_url(level?: string, source?: string, search?: string): string {
        const params = new URLSearchParams();
        if (level) params.append("level", level);
        if (source) params.append("source", source);
        if (search) params.append("search", search);
        const queryString = params.toString();
        return `api/ServerLog/entries${queryString ? "?" + queryString : ""}`;
    }

    public async getServerLogEntries(level?: string, source?: string, search?: string): Promise<LogEntry[]> {
        return (await axios.get(this.getServerLogEntries_url(level, source, search), null || undefined))
            .data as LogEntry[];
    }

    // get: api/ServerLog/sources
    public getServerLogSources_url(): string {
        return `api/ServerLog/sources`;
    }

    public async getServerLogSources(): Promise<string[]> {
        return (await axios.get(this.getServerLogSources_url(), null || undefined))
            .data as string[];
    }

    // get: api/ServerLog/levels
    public getServerLogLevels_url(): string {
        return `api/ServerLog/levels`;
    }

    public async getServerLogLevels(): Promise<string[]> {
        return (await axios.get(this.getServerLogLevels_url(), null || undefined))
            .data as string[];
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
