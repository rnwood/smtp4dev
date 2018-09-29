

export default class AttachmentSummary {

    constructor(fileName: string, contentId: string, url: string,) {

        this.fileName = fileName;
        this.contentId = contentId;
        this.url = url;
    }


    fileName: string;
    contentId: string;
    url: string;
}