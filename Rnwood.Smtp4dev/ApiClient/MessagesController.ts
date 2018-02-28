﻿import MessageSummary from './MessageSummary';
import Message from './Message';
import BaseUrlProvider from '../BaseUrlProvider';
import FileStreamResult from './FileStreamResult';
import axios from "axios";

export default class MessagesController {
    public _baseUrl: string;
 
    constructor() {        
        this._baseUrl = new BaseUrlProvider().getBaseUrl();
    }
            
    // get: api/Messages       
    public async getSummaries(): Promise<MessageSummary[]> {
        let route = () => `${this._baseUrl}api/Messages`;

        return (await axios.get(route(), null || undefined)).data as MessageSummary[];
    }
    // get: api/Messages/${encodeURIComponent(id)}       
    public async getMessage(id: string): Promise<Message> {
        let route = (id: string) => `${this._baseUrl}api/Messages/${encodeURIComponent(id)}`;

        return (await axios.get(route(id), null || undefined)).data as Message;
    }
    // get: api/Messages/${encodeURIComponent(id)}/part/${encodeURIComponent(cid)}/content       
    public async getPartContent(id: string, cid: string): Promise<FileStreamResult> {
        let route = (id: string, cid: string) => `${this._baseUrl}api/Messages/${encodeURIComponent(id)}/part/${encodeURIComponent(cid)}/content`;

        return (await axios.get(route(id, cid), null || undefined)).data as FileStreamResult;
    }
    // get: api/Messages/${encodeURIComponent(id)}/html       
    public async getMessageHtml(id: string): Promise<string> {
        let route = (id: string) => `${this._baseUrl}api/Messages/${encodeURIComponent(id)}/html`;

        return (await axios.get(route(id), null || undefined)).data as string;
    }
    // delete: api/Messages/*       
    public async deleteAll(): Promise<void> {
        let route = () => `${this._baseUrl}api/Messages/*`;

        return (await axios.delete(route(), null || undefined)).data as void;
    }
}