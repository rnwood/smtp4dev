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
    
    refresh = debounce(this.refreshInternal, 500);
    
    async refreshInternal(suppressNotifications: boolean) {
        
        const messagesByDate = await new MessagesController().getNewSummaries(this.lastNotifiedMessage?.id ?? "");
        const unnotifiedMessages = messagesByDate;
        
        if (unnotifiedMessages.length) {

            this.lastNotifiedMessage = unnotifiedMessages[0];
            unnotifiedMessages.reverse();

            if (!suppressNotifications && Notification.permission == "granted") {
                if (this.visibleNotificationCloseTimeout) {
                    clearTimeout(this.visibleNotificationCloseTimeout);
                }

                const notification = this.currentNotification = new Notification("smtp4dev: " + unnotifiedMessages.length + " new message(s) received.", {
                    body: unnotifiedMessages.slice(0, 5).map(m => "From: " + m.from + " - " + m.subject).join("\n") + (unnotifiedMessages.length > 5 ? "..." : ""),
                    tag: "newmessages",
                    renotify: true,
                    silent: (!!this.currentNotification),
                    requireInteraction: false
                });
                notification.onclick = () => {
                    this.onClick(unnotifiedMessages[0]);
                };
                notification.onclose = () => {
                    if (notification === this.currentNotification) {
                        this.currentNotification = null;
                    }
                };
                this.visibleNotificationCloseTimeout = setTimeout(() => {
                    if (notification === this.currentNotification) {
                        this.currentNotification.close();
                    }
                }, 10000);
            }
        }
    }
}