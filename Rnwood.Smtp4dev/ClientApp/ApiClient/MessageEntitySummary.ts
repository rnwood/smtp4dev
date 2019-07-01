 
//Header[] from MessageEntitySummary
import Header from './Header';
//AttachmentSummary[] from MessageEntitySummary
import AttachmentSummary from './AttachmentSummary';
export default class MessageEntitySummary {

    constructor(id: string, headers: Header[], childParts: MessageEntitySummary[], name: string, messageId: string, contentId: string, attachments: AttachmentSummary[], size: number, ) {
         
        this.id = id; 
        this.headers = headers; 
        this.childParts = childParts; 
        this.name = name; 
        this.messageId = messageId; 
        this.contentId = contentId; 
        this.attachments = attachments; 
        this.size = size;
    }

     
    id: string; 
    headers: Header[]; 
    childParts: MessageEntitySummary[]; 
    name: string; 
    messageId: string; 
    contentId: string; 
    attachments: AttachmentSummary[]; 
    size: number;
}
