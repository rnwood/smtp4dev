

import SessionSummary from "./SessionSummary";
import Session from "./Session";
import axios from "axios";

export default class SessionsController {
    _baseUrl: string;

    constructor(baseUrl: string = "/") {
        this._baseUrl = baseUrl;
    }


    // get: api/Sessions  
    getSummaries_url(): string {
        return `${this._baseUrl}api/Sessions`;
    }

    async getSummaries(): Promise<SessionSummary[]> {

        return (await axios.get(this.getSummaries_url(), null || undefined)).data as SessionSummary[];
    }

    // get: api/Sessions/${encodeURIComponent(id)}  
    getSession_url(id: string): string {
        return `${this._baseUrl}api/Sessions/${encodeURIComponent(id)}`;
    }

    async getSession(id: string): Promise<Session> {

        return (await axios.get(this.getSession_url(id), null || undefined)).data as Session;
    }

    // delete: api/Sessions/${encodeURIComponent(id)}  
    delete_url(id: string): string {
        return `${this._baseUrl}api/Sessions/${encodeURIComponent(id)}`;
    }

    async delete(id: string): Promise<void> {

        return (await axios.delete(this.delete_url(id), null || undefined)).data as void;
    }

    // delete: api/Sessions/*  
    deleteAll_url(): string {
        return `${this._baseUrl}api/Sessions/*`;
    }

    async deleteAll(): Promise<void> {

        return (await axios.delete(this.deleteAll_url(), null || undefined)).data as void;
    }
}