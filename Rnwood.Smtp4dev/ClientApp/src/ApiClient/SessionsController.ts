import SessionSummary from "./SessionSummary";
import Session from "./Session";
import axios from "axios";
import PagedResult from "./PagedResult";

export default class SessionsController {
    constructor() {}

    // get: api/Sessions
    public getSummaries_url(page: number = 1, pageSize: number = 25): string {
        return `api/Sessions?page=${page}&pageSize=${pageSize}`;
    }

    public async getSummaries(
        page: number,
        pageSize: number
    ): Promise<PagedResult<SessionSummary>> {
        return (
            await axios.get(
                this.getSummaries_url(page, pageSize),
                null || undefined
            )
        ).data as PagedResult<SessionSummary>;
    }

    // get: api/Sessions/${encodeURIComponent(id)}
    public getSession_url(id: string): string {
        return `api/Sessions/${encodeURIComponent(id)}`;
    }

    public async getSession(id: string): Promise<Session> {
        return (await axios.get(this.getSession_url(id), null || undefined))
            .data as Session;
    }

    // get: api/Sessions/${encodeURIComponent(id)}/log
    public getSessionLog_url(id: string): string {
        return `api/Sessions/${encodeURIComponent(id)}/log`;
    }

    public async getSessionLog(id: string): Promise<string> {
        return (await axios.get(this.getSessionLog_url(id), null || undefined))
            .data as string;
    }

    // delete: api/Sessions/${encodeURIComponent(id)}
    public delete_url(id: string): string {
        return `api/Sessions/${encodeURIComponent(id)}`;
    }

    public async delete(id: string): Promise<void> {
        return (await axios.delete(this.delete_url(id), null || undefined))
            .data as void;
    }

    // delete: api/Sessions/*
    public deleteAll_url(): string {
        return `api/Sessions/*`;
    }

    public async deleteAll(): Promise<void> {
        return (await axios.delete(this.deleteAll_url(), null || undefined))
            .data as void;
    }
}
