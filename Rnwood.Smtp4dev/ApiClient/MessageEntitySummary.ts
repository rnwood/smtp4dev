 
//Header[] from MessageEntitySummary
import Header from './Header';
//AttachmentSummary[] from MessageEntitySummary
import AttachmentSummary from './AttachmentSummary';
export default class MessageEntitySummary {  
    headers: Header[]; 
    childParts: MessageEntitySummary[]; 
    name: string; 
    messageId: string; 
    contentId: string; 
    attachments: AttachmentSummary[];
}
