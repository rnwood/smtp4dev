import MessageSummary from "./ApiClient/MessageSummary";
import { debounce } from 'ts-debounce';

export default class MessageNotificationManager {
    constructor(onClick: (message: MessageSummary) => void) {
        if (Notification.permission == "default") {
            Notification.requestPermission();
        }
        this.onClick = onClick;
    }

    private lastNotifiedMessage: MessageSummary | null = null;
    private onClick: (message: MessageSummary) => void;
    private visibleNotificationCloseTimeout: number | null = null;
    private currentNotification: Notification | null = null;

    setInitialMessages(messages: MessageSummary[]) {
        let messagesByDate = this.sortMessages(messages);
        this.lastNotifiedMessage = messagesByDate[messagesByDate.length - 1];
    }

    notifyMessages(messages: MessageSummary[]) {

        this.updateNotificationsDebounced(messages);
    }

    private updateNotificationsDebounced = debounce(this.updateNotifications, 500);

    private updateNotifications(messages: MessageSummary[]) {
        //Sort by something stable. Multiple messages may be received at same instant so use id as secondary
        let messagesByDate = this.sortMessages(messages);
        var unnotifiedMessages: MessageSummary[];
        if (!this.lastNotifiedMessage) {
            unnotifiedMessages = messagesByDate;
        }
        else {
            var indexOfLastNotifiedMessage = messagesByDate.indexOf(this.lastNotifiedMessage);
            if (indexOfLastNotifiedMessage != -1) {
                unnotifiedMessages = messagesByDate.slice(indexOfLastNotifiedMessage + 1);
            }
            else {
                unnotifiedMessages = messagesByDate;
            }
        }
        if (unnotifiedMessages.length) {

            unnotifiedMessages.reverse();

            if (Notification.permission != "granted") {
                //Ensure that if notification permission is granted later on, existing messages are treated as already notified.
                this.lastNotifiedMessage = unnotifiedMessages[0];
            }
            else {
                if (this.visibleNotificationCloseTimeout) {
                    clearTimeout(this.visibleNotificationCloseTimeout);
                }

                var notification = this.currentNotification = new Notification("smtp4dev: " + unnotifiedMessages.length + " new message(s) received.", {
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
                        this.lastNotifiedMessage = unnotifiedMessages[0];
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

    private sortMessages(messages: MessageSummary[]) {
        let messagesByDate = messages.slice(0, messages.length);
        messagesByDate.sort((m1, m2) => {
            if (m1.receivedDate == m2.receivedDate) {
                if (m1.id > m2.id) {
                    return 1;
                }
                else {
                    return -1;
                }
            }
            else if (m1.receivedDate > m2.receivedDate) {
                return 1;
            }
            else {
                return -1;
            }
        });
        return messagesByDate;
    }
}