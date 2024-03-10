

import MessageSummary from './MessageSummary';
import Message from './Message';
import FileStreamResult from './FileStreamResult';
import MessageRelayOptions from './MessageRelayOptions';
import axios from "axios";
import PagedResult from './PagedResult';

export default class MessagesController {
               
    constructor(){
    }
    
    private apiBaseUrl = `api/Messages`;

    public getSummaries_url(sortColumn: string, sortIsDescending: boolean, page: number = 1, pageSize: number = 25): string {
        return `${this.apiBaseUrl}?sortColumn=${encodeURIComponent(sortColumn)}&sortIsDescending=${sortIsDescending}&page=${page}&pageSize=${pageSize}`;
    }

    public async getSummaries(sortColumn: string, sortIsDescending: boolean, page: number = 1, pageSize: number = 25): Promise<PagedResult<MessageSummary>> {

        return (await axios.get(this.getSummaries_url(sortColumn, sortIsDescending, page,pageSize), null || undefined)).data as PagedResult<MessageSummary>;
    }
    
    // get: api/Messages/${encodeURIComponent(id)}  
    public getMessage_url(id: string): string {
        return `${this.apiBaseUrl}/${encodeURIComponent(id)}`;
    }

    public async getMessage(id: string): Promise<Message> {

        return (await axios.get(this.getMessage_url(id), null || undefined)).data as Message;
    }
    
    // post: api/Messages/${encodeURIComponent(id)}  
    public markMessageRead_url(id: string): string {
        return `${this.apiBaseUrl}/${encodeURIComponent(id)}`;
    }

    public async markMessageRead(id: string): Promise<void> {

        return (await axios.post(this.markMessageRead_url(id), null || undefined)).data as void;
    }

    public async markAllMessageRead(): Promise<void> {
        return await axios.post(`${this.apiBaseUrl}/markAllRead`);
    }
    
    // get: api/Messages/${encodeURIComponent(id)}/download  
    public downloadMessage_url(id: string): string {
        return `${this.apiBaseUrl}/${encodeURIComponent(id)}/download`;
    }

    public async downloadMessage(id: string): Promise<FileStreamResult> {

        return (await axios.get(this.downloadMessage_url(id), null || undefined)).data as FileStreamResult;
    }
    
    // post: api/Messages/${encodeURIComponent(id)}/relay  
    public relayMessage_url(id: string): string {
        return `${this.apiBaseUrl}/${encodeURIComponent(id)}/relay`;
    }

    public async relayMessage(id: string, options: MessageRelayOptions): Promise<void> {

        return (await axios.post(this.relayMessage_url(id), options || undefined)).data as void;
    }
    
    // get: api/Messages/${encodeURIComponent(id)}/part/${encodeURIComponent(partid)}/content  
    public getPartContent_url(id: string, partid: string): string {
        return `${this.apiBaseUrl}/${encodeURIComponent(id)}/part/${encodeURIComponent(partid)}/content`;
    }

    public async getPartContent(id: string, partid: string): Promise<FileStreamResult> {

        return (await axios.get(this.getPartContent_url(id, partid), null || undefined)).data as FileStreamResult;
    }
    
    // get: api/Messages/${encodeURIComponent(id)}/part/${encodeURIComponent(partid)}/source  
    public getPartSource_url(id: string, partid: string): string {
        return `${this.apiBaseUrl}/${encodeURIComponent(id)}/part/${encodeURIComponent(partid)}/source`;
    }

    public async getPartSource(id: string, partid: string): Promise<string> {

        return (await axios.get(this.getPartSource_url(id, partid), null || undefined)).data as string;
    }
    
    // get: api/Messages/${encodeURIComponent(id)}/part/${encodeURIComponent(partid)}/raw  
    public getPartSourceRaw_url(id: string, partid: string): string {
        return `${this.apiBaseUrl}/${encodeURIComponent(id)}/part/${encodeURIComponent(partid)}/raw`;
    }

    public async getPartSourceRaw(id: string, partid: string): Promise<string> {

        return (await axios.get(this.getPartSourceRaw_url(id, partid), null || undefined)).data as string;
    }
    
    // get: api/Messages/${encodeURIComponent(id)}/raw  
    public getMessageSourceRaw_url(id: string): string {
        return `${this.apiBaseUrl}/${encodeURIComponent(id)}/raw`;
    }

    public async getMessageSourceRaw(id: string): Promise<string> {

        return (await axios.get(this.getMessageSourceRaw_url(id), null || undefined)).data as string;
    }
    
    // get: api/Messages/${encodeURIComponent(id)}/source  
    public getMessageSource_url(id: string): string {
        return `${this.apiBaseUrl}/${encodeURIComponent(id)}/source`;
    }

    public async getMessageSource(id: string): Promise<string> {

        return (await axios.get(this.getMessageSource_url(id), null || undefined)).data as string;
    }
    
    // get: api/Messages/${encodeURIComponent(id)}/html  
    public getMessageHtml_url(id: string): string {
        return `${this.apiBaseUrl}/${encodeURIComponent(id)}/html`;
    }

    public async getMessageHtml(id: string): Promise<string> {

        return (await axios.get(this.getMessageHtml_url(id), null || undefined)).data as string;
    }

    // get: api/Messages/${encodeURIComponent(id)}/plaintext  
    public getMessagePlainText_url(id: string): string {
        return `${this.apiBaseUrl}/${encodeURIComponent(id)}/plaintext`;
    }

    public async getMessagePlainText(id: string): Promise<string> {

        return (await axios.get(this.getMessagePlainText_url(id), null || undefined)).data as string;
    }
    
    // delete: api/Messages/${encodeURIComponent(id)}  
    public delete_url(id: string): string {
        return `${this.apiBaseUrl}/${encodeURIComponent(id)}`;
    }

    public async delete(id: string): Promise<void> {

        return (await axios.delete(this.delete_url(id), null || undefined)).data as void;
    }
    
    // delete: api/Messages/*  
    public deleteAll_url(): string {
        return `${this.apiBaseUrl}/*`;
    }

    public async deleteAll(): Promise<void> {

        return (await axios.delete(this.deleteAll_url(), null || undefined)).data as void;
    }
}