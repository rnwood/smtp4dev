 
//Header[] from MessageEntitySummary
import Header from './Header';
//AttachmentSummary[] from MessageEntitySummary
import AttachmentSummary from './AttachmentSummary';
export default class MessageEntitySummary {

    constructor(headers: Header[], childParts: MessageEntitySummary[], name: string, messageId: string, id: string, contentId: string, attachments: AttachmentSummary[], size: number, ) {
         
        this.headers = headers; 
        this.childParts = childParts; 
        this.name = name; 
        this.messageId = messageId; 
        this.id = id;
        this.contentId = contentId; 
        this.attachments = attachments; 
        this.size = size;
    }

     
    headers: Header[]; 
    childParts: MessageEntitySummary[]; 
    name: string; 
    messageId: string; 
    id: string;
    contentId: string; 
    attachments: AttachmentSummary[]; 
    size: number;
}
