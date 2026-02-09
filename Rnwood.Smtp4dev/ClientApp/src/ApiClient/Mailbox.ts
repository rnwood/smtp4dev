
export class HeaderFilter {
    header: string;
    pattern: string;
 
    constructor(header: string = '', pattern: string = '') {
        this.header = header;
        this.pattern = pattern;
    }
}

export class SourceFilter {
    pattern: string;
 
    constructor(pattern: string = '') {
        this.pattern = pattern;
    }
}

export default class Mailbox {
    name: string;
    recipients: string;
    headerFilters?: HeaderFilter[];
    sourceFilters?: SourceFilter[];
 
    constructor(name: string, recipients: string, headerFilters?: HeaderFilter[], sourceFilters?: SourceFilter[]) {
         
        this.name = name;
        this.recipients = recipients;
        this.headerFilters = headerFilters;
        this.sourceFilters = sourceFilters;
    }


}
