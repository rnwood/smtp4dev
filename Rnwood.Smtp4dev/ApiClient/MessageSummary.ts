

export default class MessageSummary {

    constructor(id: string, from: string, to: string, receivedDate: Date, subject: string,) {

        this.id = id;
        this.from = from;
        this.to = to;
        this.receivedDate = receivedDate;
        this.subject = subject;
    }


    id: string;
    from: string;
    to: string;
    receivedDate: Date;
    subject: string;
}