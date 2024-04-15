import MessageSummary from "./ApiClient/MessageSummary";
import { debounce } from 'ts-debounce';
import MessagesController from "./ApiClient/MessagesController";

export default class MessageNotificationManager {
    constructor(onClick: (message: MessageSummary) => void) {
        if (Notification.permission == "default") {
            Notification.requestPermission();
        }
        this.onClick = onClick;
    }

    private lastNotifiedMessage: MessageSummary | null = null;
    private onClick: (message: MessageSummary) => void;
    private visibleNotificationCloseTimeout: any | null = null;
    private currentNotification: Notification | null = null;
    private unnotifiedMessages: MessageSummary[] = [];
    private currentNotificationMessages: MessageSummary[] = [];
    
    refresh = debounce(this.refreshInternal, 500);
    
    async refreshInternal(suppressNotifications: boolean) {
        
        const messagesByDate = await new MessagesController().getNewSummaries(this.lastNotifiedMessage?.id ?? "");
        const messagesToAdd = messagesByDate.filter(m => !this.unnotifiedMessages.find(um => um.id=== m.id));

        
        if (messagesToAdd.length) {

            this.unnotifiedMessages = messagesToAdd.concat(this.unnotifiedMessages);
            this.lastNotifiedMessage = this.unnotifiedMessages[0];

            if (!suppressNotifications && Notification.permission == "granted") {
                if (this.visibleNotificationCloseTimeout) {
                    clearTimeout(this.visibleNotificationCloseTimeout);
                }

                const notification = this.currentNotification = new Notification("smtp4dev: " + this.unnotifiedMessages.length + " new message(s) received.", {
                    body: this.unnotifiedMessages.slice(0, 5).map(m => "From: " + m.from + " - " + m.subject).join("\n") + (this.unnotifiedMessages.length > 5 ? "..." : ""),
                    tag: "newmessages",
                    silent: (!!this.currentNotification),
                    requireInteraction: false
                });
                this.currentNotificationMessages = this.unnotifiedMessages;
                notification.onclick = () => {
                    this.onClick(this.unnotifiedMessages[0]);
                };
                notification.onclose = () => {
                    if (notification === this.currentNotification) {
                        this.currentNotification = null;
                        this.unnotifiedMessages =[];
                        this.currentNotificationMessages = [];
                    }
                };
                this.visibleNotificationCloseTimeout = setTimeout(() => {
                    if (notification === this.currentNotification) {
                        this.currentNotification.close();
                    }
                }, 10000);
            } else {
                this.unnotifiedMessages =[];
            }
        }
    }
}