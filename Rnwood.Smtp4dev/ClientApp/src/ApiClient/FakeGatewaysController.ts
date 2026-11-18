import axios from "axios";

// A dedicated instance on purpose: the global axios response interceptor (main.ts) converts every
// ISO-8601 looking string into a Date, which would corrupt request data that must be shown verbatim
// (Targito data values, raw params) and the receivedAt strings.
const http = axios.create();

export interface FakeGatewaysHealth {
    status: string;
    features: string[];
    version: string | null;
}

export interface SmsMessage {
    id: string;
    receivedAt: string;
    to: string;
    sender: string | null;
    text: string;
    contentType: string | null;
    sendTime: string | null;
    externalSmsId: string | null;
    gatewayUser: string | null;
    status: string;
    msgId: string;
    encoding: string;
    segments: number;
    code: string | null;
}

export interface TargitoRecord {
    id: string;
    receivedAt: string;
    method: string;
    email: string | null;
    oldEmail: string | null;
    templateId: string | null;
    templateName: string | null;
    senderDomain: string | null;
    sendDateTime: string | null;
    accountUser: string | null;
    data: { [key: string]: string | null };
    params: any;
    resultId: string | null;
}

export default class FakeGatewaysController {
    constructor() {}

    // get: api/fakegateways/health
    public getHealth_url(): string {
        return `api/fakegateways/health`;
    }

    public async getHealth(): Promise<FakeGatewaysHealth> {
        return (await http.get(this.getHealth_url())).data as FakeGatewaysHealth;
    }

    // get: api/fakegateways/sms
    public getSmsList_url(): string {
        return `api/fakegateways/sms`;
    }

    public async getSmsList(): Promise<SmsMessage[]> {
        return (await http.get(this.getSmsList_url())).data as SmsMessage[];
    }

    // get: api/fakegateways/sms/{id}
    public getSms_url(id: string): string {
        return `api/fakegateways/sms/${encodeURIComponent(id)}`;
    }

    public async getSms(id: string): Promise<SmsMessage> {
        return (await http.get(this.getSms_url(id))).data as SmsMessage;
    }

    // delete: api/fakegateways/sms
    public deleteAllSms_url(): string {
        return `api/fakegateways/sms`;
    }

    public async deleteAllSms(): Promise<void> {
        await http.delete(this.deleteAllSms_url());
    }

    // delete: api/fakegateways/sms/{id}
    public deleteSms_url(id: string): string {
        return `api/fakegateways/sms/${encodeURIComponent(id)}`;
    }

    public async deleteSms(id: string): Promise<void> {
        await http.delete(this.deleteSms_url(id));
    }

    // get: api/fakegateways/targito
    public getTargitoList_url(): string {
        return `api/fakegateways/targito`;
    }

    public async getTargitoList(): Promise<TargitoRecord[]> {
        return (await http.get(this.getTargitoList_url())).data as TargitoRecord[];
    }

    // get: api/fakegateways/targito/{id}
    public getTargito_url(id: string): string {
        return `api/fakegateways/targito/${encodeURIComponent(id)}`;
    }

    public async getTargito(id: string): Promise<TargitoRecord> {
        return (await http.get(this.getTargito_url(id))).data as TargitoRecord;
    }

    // delete: api/fakegateways/targito
    public deleteAllTargito_url(): string {
        return `api/fakegateways/targito`;
    }

    public async deleteAllTargito(): Promise<void> {
        await http.delete(this.deleteAllTargito_url());
    }

    // delete: api/fakegateways/targito/{id}
    public deleteTargito_url(id: string): string {
        return `api/fakegateways/targito/${encodeURIComponent(id)}`;
    }

    public async deleteTargito(id: string): Promise<void> {
        await http.delete(this.deleteTargito_url(id));
    }
}
