import axios from "axios";
import Info from "./Info";

export default class InfoController {
    constructor() {}

    public getInfo_url(): string {
        return `api/Info`;
    }

    public async getInfo(): Promise<Info> {
        return (await axios.get(this.getInfo_url(), null || undefined))
            .data as Info;
    }
}
