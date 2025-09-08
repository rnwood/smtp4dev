export default class ClientSettings {
    constructor(pageSize: number, autoViewNewMessages: boolean = false) {
        this.pageSize = pageSize;
        this.autoViewNewMessages = autoViewNewMessages;
    }

    pageSize: number;
    autoViewNewMessages: boolean;
}
