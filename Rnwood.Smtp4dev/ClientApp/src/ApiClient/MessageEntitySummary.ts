
//Header[] from MessageEntitySummary
import Header from './Header';
//AttachmentSummary[] from MessageEntitySummary
import AttachmentSummary from './AttachmentSummary';
//MessageWarning[] from MessageEntitySummary
import MessageWarning from './MessageWarning';
export default class MessageEntitySummary {
 
    constructor(id: string, headers: Header[], childParts: MessageEntitySummary[], name: string, messageId: string, contentId: string, attachments: AttachmentSummary[], warnings: MessageWarning[], size: number, isAttachment: boolean, ) {
         
        this.id = id; 
        this.headers = headers; 
        this.childParts = childParts; 
        this.name = name; 
        this.messageId = messageId; 
        this.contentId = contentId; 
        this.attachments = attachments; 
        this.warnings = warnings; 
        this.size = size; 
        this.isAttachment = isAttachment;
    }

     
    id: string; 
    headers: Header[]; 
    childParts: MessageEntitySummary[]; 
    name: string; 
    messageId: string; 
    contentId: string; 
    attachments: AttachmentSummary[]; 
    warnings: MessageWarning[]; 
    size: number; 
    isAttachment: boolean;
}
