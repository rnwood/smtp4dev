export default class LogEntry {
    timestamp: Date = new Date();
    level: string = "";
    message: string = "";
    exception: string = "";
    source: string = "";
    formattedMessage: string = "";
}
