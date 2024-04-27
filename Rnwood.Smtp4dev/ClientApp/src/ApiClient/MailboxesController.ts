import axios from "axios";
import Mailbox from "./Mailbox";

export default class MailboxesController {
    constructor() {}

    public getAll_url(): string {
        return `api/Mailboxes`;
    }

    public async getAll(): Promise<Mailbox[]> {
        return (await axios.get(this.getAll_url(), null || undefined))
            .data as Mailbox[];
    }
}
