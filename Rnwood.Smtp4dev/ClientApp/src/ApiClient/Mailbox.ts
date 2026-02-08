
export class HeaderFilter {
    header: string;
    pattern: string;
 
    constructor(header: string = '', pattern: string = '') {
        this.header = header;
        this.pattern = pattern;
    }
}

export default class Mailbox {
    name: string;
    recipients: string;
    headerFilters?: HeaderFilter[];
 
    constructor(name: string, recipients: string, headerFilters?: HeaderFilter[]) {
         
        this.name = name;
        this.recipients = recipients;
        this.headerFilters = headerFilters;
    }


}
