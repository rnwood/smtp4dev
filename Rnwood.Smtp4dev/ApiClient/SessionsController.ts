 

import SessionSummary from './SessionSummary';
import Session from './Session';
import axios from "axios";

export default class SessionsController {
    public _baseUrl: string;                
 
    constructor(baseUrl: string = "/"){
        this._baseUrl = baseUrl;
    }
        
    
    // get: api/Sessions       
    public async getSummaries(): Promise<SessionSummary[]> {
        let route = () => `${this._baseUrl}api/Sessions`;

        return (await axios.get(route(), null || undefined)).data as SessionSummary[];
    }
    // get: api/Sessions/${encodeURIComponent(id)}       
    public async getSession(id: string): Promise<Session> {
        let route = (id: string) => `${this._baseUrl}api/Sessions/${encodeURIComponent(id)}`;

        return (await axios.get(route(id), null || undefined)).data as Session;
    }
    // delete: api/Sessions/*       
    public async deleteAll(): Promise<void> {
        let route = () => `${this._baseUrl}api/Sessions/*`;

        return (await axios.delete(route(), null || undefined)).data as void;
    }
}