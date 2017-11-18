 
//MessageEntitySummary[] from Message
import MessageEntitySummary from './MessageEntitySummary';
//Header[] from Message
import Header from './Header';
export default class Message {  
    id: string; 
    from: string; 
    to: string; 
    receivedDate: Date; 
    subject: string; 
    parts: MessageEntitySummary[]; 
    headers: Header[];
}
