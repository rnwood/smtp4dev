 

import MessageSummary from './MessageSummary';
import Message from './Message';
import FileStreamResult from './FileStreamResult';
import axios from "axios";

export default class MessagesController {
               
    constructor(){
    }
        
    
    
    // get: api/Messages?sortColumn=${encodeURIComponent(sortColumn)}&sortIsDescending=${sortIsDescending}  
    public getSummaries_url(sortColumn: string, sortIsDescending: boolean): string {
        return `api/Messages?sortColumn=${encodeURIComponent(sortColumn)}&sortIsDescending=${sortIsDescending}`;
    }

    public async getSummaries(sortColumn: string, sortIsDescending: boolean): Promise<MessageSummary[]> {

        return (await axios.get(this.getSummaries_url(sortColumn, sortIsDescending), null || undefined)).data as MessageSummary[];
    }
    
    // get: api/Messages/${encodeURIComponent(id)}  
    public getMessage_url(id: string): string {
        return `api/Messages/${encodeURIComponent(id)}`;
    }

    public async getMessage(id: string): Promise<Message> {

        return (await axios.get(this.getMessage_url(id), null || undefined)).data as Message;
    }
    
    // post: api/Messages/${encodeURIComponent(id)}  
    public markMessageRead_url(id: string): string {
        return `api/Messages/${encodeURIComponent(id)}`;
    }

    public async markMessageRead(id: string): Promise<void> {

        return (await axios.post(this.markMessageRead_url(id), null || undefined)).data as void;
    }
    
    // get: api/Messages/${encodeURIComponent(id)}/source  
    public downloadMessage_url(id: string): string {
        return `api/Messages/${encodeURIComponent(id)}/source`;
    }

    public async downloadMessage(id: string): Promise<FileStreamResult> {

        return (await axios.get(this.downloadMessage_url(id), null || undefined)).data as FileStreamResult;
    }
    
    // get: api/Messages/${encodeURIComponent(id)}/part/${encodeURIComponent(partid)}/content  
    public getPartContent_url(id: string, partid: string): string {
        return `api/Messages/${encodeURIComponent(id)}/part/${encodeURIComponent(partid)}/content`;
    }

    public async getPartContent(id: string, partid: string): Promise<FileStreamResult> {

        return (await axios.get(this.getPartContent_url(id, partid), null || undefined)).data as FileStreamResult;
    }
    
    // get: api/Messages/${encodeURIComponent(id)}/part/${encodeURIComponent(partid)}/source  
    public getPartSource_url(id: string, partid: string): string {
        return `api/Messages/${encodeURIComponent(id)}/part/${encodeURIComponent(partid)}/source`;
    }

    public async getPartSource(id: string, partid: string): Promise<string> {

        return (await axios.get(this.getPartSource_url(id, partid), null || undefined)).data as string;
    }
    
    // get: api/Messages/${encodeURIComponent(id)}/part/${encodeURIComponent(partid)}/raw  
    public getPartSourceRaw_url(id: string, partid: string): string {
        return `api/Messages/${encodeURIComponent(id)}/part/${encodeURIComponent(partid)}/raw`;
    }

    public async getPartSourceRaw(id: string, partid: string): Promise<string> {

        return (await axios.get(this.getPartSourceRaw_url(id, partid), null || undefined)).data as string;
    }
    
    // get: api/Messages/${encodeURIComponent(id)}/html  
    public getMessageHtml_url(id: string): string {
        return `api/Messages/${encodeURIComponent(id)}/html`;
    }

    public async getMessageHtml(id: string): Promise<string> {

        return (await axios.get(this.getMessageHtml_url(id), null || undefined)).data as string;
    }
    
    // delete: api/Messages/${encodeURIComponent(id)}  
    public delete_url(id: string): string {
        return `api/Messages/${encodeURIComponent(id)}`;
    }

    public async delete(id: string): Promise<void> {

        return (await axios.delete(this.delete_url(id), null || undefined)).data as void;
    }
    
    // delete: api/Messages/*  
    public deleteAll_url(): string {
        return `api/Messages/*`;
    }

    public async deleteAll(): Promise<void> {

        return (await axios.delete(this.deleteAll_url(), null || undefined)).data as void;
    }
}