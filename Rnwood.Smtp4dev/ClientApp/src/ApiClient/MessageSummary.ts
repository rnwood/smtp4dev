export default class MessageSummary {
  deliveredTo: string[];
  constructor(
    id: string,
    from: string,
      to: string[],
    deliveredTo: string[],
    receivedDate: Date,
    subject: string,
    attachmentCount: number,
    isUnread: boolean,
    isRelayed: boolean,
  ) {
    this.id = id;
    this.from = from;
      this.to = to;
      this.deliveredTo = to;
    this.receivedDate = receivedDate;
    this.subject = subject;
    this.attachmentCount = attachmentCount;
    this.isUnread = isUnread;
    this.isRelayed = isRelayed;
  }

  id: string;
  from: string;
  to: string[];
  receivedDate: Date;
  subject: string;
  attachmentCount: number;
  isUnread: boolean;
  isRelayed: boolean;
}
