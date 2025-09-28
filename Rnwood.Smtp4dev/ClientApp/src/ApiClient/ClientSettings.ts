export default class ClientSettings {
    constructor(pageSize: number, autoViewNewMessages: boolean = false, darkMode: string = "follow") {
        this.pageSize = pageSize;
        this.autoViewNewMessages = autoViewNewMessages;
        this.darkMode = darkMode;
    }

    pageSize: number;
    autoViewNewMessages: boolean;
    darkMode: string;
}
