 
//MessageEntitySummary[] from Message
import MessageEntitySummary from './MessageEntitySummary';
//Header[] from Message
import Header from './Header';
export default class Message {

    constructor(id: string, from: string, to: string, receivedDate: Date, subject: string, parts: MessageEntitySummary[], headers: Header[], ) {
         
        this.id = id; 
        this.from = from; 
        this.to = to; 
        this.receivedDate = receivedDate; 
        this.subject = subject; 
        this.parts = parts; 
        this.headers = headers;
    }

     
    id: string; 
    from: string; 
    to: string; 
    receivedDate: Date; 
    subject: string; 
    parts: MessageEntitySummary[]; 
    headers: Header[];
}
