 

export default class AttachmentSummary {

    constructor(fileName: string, contentId: string, id: string, url: string, ) {
         
        this.fileName = fileName; 
        this.contentId = contentId; 
        this.id = id; 
        this.url = url;
    }

     
    fileName: string; 
    contentId: string; 
    id: string; 
    url: string;
}
