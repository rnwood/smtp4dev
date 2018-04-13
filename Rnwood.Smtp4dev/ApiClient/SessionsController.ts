 

import SessionSummary from './SessionSummary';
import BaseUrlProvider from '../BaseUrlProvider';
import Session from './Session';
import axios from "axios";

export default class SessionsController {
    public _baseUrl: string;                
 
    constructor(){
        this._baseUrl = new BaseUrlProvider().getBaseUrl();;
    }
        
    
    
    // get: api/Sessions  
    public getSummaries_url(): string {
        return `${this._baseUrl}api/Sessions`;
    }

    public async getSummaries(): Promise<SessionSummary[]> {

        return (await axios.get(this.getSummaries_url(), null || undefined)).data as SessionSummary[];
    }
    
    // get: api/Sessions/${encodeURIComponent(id)}  
    public getSession_url(id: string): string {
        return `${this._baseUrl}api/Sessions/${encodeURIComponent(id)}`;
    }

    public async getSession(id: string): Promise<Session> {

        return (await axios.get(this.getSession_url(id), null || undefined)).data as Session;
    }
    
    // delete: api/Sessions/${encodeURIComponent(id)}  
    public delete_url(id: string): string {
        return `${this._baseUrl}api/Sessions/${encodeURIComponent(id)}`;
    }

    public async delete(id: string): Promise<void> {

        return (await axios.delete(this.delete_url(id), null || undefined)).data as void;
    }
    
    // delete: api/Sessions/*  
    public deleteAll_url(): string {
        return `${this._baseUrl}api/Sessions/*`;
    }

    public async deleteAll(): Promise<void> {

        return (await axios.delete(this.deleteAll_url(), null || undefined)).data as void;
    }
}