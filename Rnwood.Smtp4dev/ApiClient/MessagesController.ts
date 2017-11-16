

import MessageHeader from './MessageHeader';
import Message from './Message';
import axios from "axios";

export default class MessagesController {
    public _baseUrl: string;                
 
    constructor(baseUrl: string = "/"){
        this._baseUrl = baseUrl;
    }
        
    
    // get: api/Messages       
    public async getHeaders(): Promise<MessageHeader[]> {
        let route = () => `${this._baseUrl}api/Messages`;

        return (await axios.get(route(), null || undefined)).data as MessageHeader[];
    }
    // get: api/Messages/${encodeURIComponent(id)}       
    public async getMessage(id: string): Promise<Message> {
        let route = (id: string) => `${this._baseUrl}api/Messages/${encodeURIComponent(id)}`;

        return (await axios.get(route(id), null || undefined)).data as Message;
    }
    // delete: api/Messages/*       
    public async deleteAll(): Promise<void> {
        let route = () => `${this._baseUrl}api/Messages/*`;

        return (await axios.delete(route(), null || undefined)).data as void;
    }
}