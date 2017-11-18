 
//Header[] from MessageEntitySummary
import Header from './Header';
export default class MessageEntitySummary {  
    headers: Header[]; 
    childParts: MessageEntitySummary[]; 
    name: string; 
    html: string; 
    source: string; 
    body: string;
}
