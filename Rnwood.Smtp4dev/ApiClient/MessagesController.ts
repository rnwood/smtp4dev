

import MessageSummary from "./MessageSummary";
import Message from "./Message";
import FileStreamResult from "./FileStreamResult";
import axios from "axios";

export default class MessagesController {
    _baseUrl: string;

    constructor(baseUrl: string = "/") {
        this._baseUrl = baseUrl;
    }


    // get: api/Messages  
    getSummaries_url(): string {
        return `${this._baseUrl}api/Messages`;
    }

    async getSummaries(): Promise<MessageSummary[]> {

        return (await axios.get(this.getSummaries_url(), null || undefined)).data as MessageSummary[];
    }

    // get: api/Messages/${encodeURIComponent(id)}  
    getMessage_url(id: string): string {
        return `${this._baseUrl}api/Messages/${encodeURIComponent(id)}`;
    }

    async getMessage(id: string): Promise<Message> {

        return (await axios.get(this.getMessage_url(id), null || undefined)).data as Message;
    }

    // get: api/Messages/${encodeURIComponent(id)}/source  
    downloadMessage_url(id: string): string {
        return `${this._baseUrl}api/Messages/${encodeURIComponent(id)}/source`;
    }

    async downloadMessage(id: string): Promise<FileStreamResult> {

        return (await axios.get(this.downloadMessage_url(id), null || undefined)).data as FileStreamResult;
    }

    // get: api/Messages/${encodeURIComponent(id)}/part/${encodeURIComponent(cid)}/content  
    getPartContent_url(id: string, cid: string): string {
        return `${this._baseUrl}api/Messages/${encodeURIComponent(id)}/part/${encodeURIComponent(cid)}/content`;
    }

    async getPartContent(id: string, cid: string): Promise<FileStreamResult> {

        return (await axios.get(this.getPartContent_url(id, cid), null || undefined)).data as FileStreamResult;
    }

    // get: api/Messages/${encodeURIComponent(id)}/part/${encodeURIComponent(cid)}/source  
    getPartSource_url(id: string, cid: string): string {
        return `${this._baseUrl}api/Messages/${encodeURIComponent(id)}/part/${encodeURIComponent(cid)}/source`;
    }

    async getPartSource(id: string, cid: string): Promise<string> {

        return (await axios.get(this.getPartSource_url(id, cid), null || undefined)).data as string;
    }

    // get: api/Messages/${encodeURIComponent(id)}/html  
    getMessageHtml_url(id: string): string {
        return `${this._baseUrl}api/Messages/${encodeURIComponent(id)}/html`;
    }

    async getMessageHtml(id: string): Promise<string> {

        return (await axios.get(this.getMessageHtml_url(id), null || undefined)).data as string;
    }

    // delete: api/Messages/${encodeURIComponent(id)}  
    delete_url(id: string): string {
        return `${this._baseUrl}api/Messages/${encodeURIComponent(id)}`;
    }

    async delete(id: string): Promise<void> {

        return (await axios.delete(this.delete_url(id), null || undefined)).data as void;
    }

    // delete: api/Messages/*  
    deleteAll_url(): string {
        return `${this._baseUrl}api/Messages/*`;
    }

    async deleteAll(): Promise<void> {

        return (await axios.delete(this.deleteAll_url(), null || undefined)).data as void;
    }
}