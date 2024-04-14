import axios from "axios";
import VersionInfo from "./VersionInfo";

export default class VersionController {
    constructor() {}

    public getVersion_url(): string {
        return `api/Version`;
    }

    public async getVersion(): Promise<VersionInfo> {
        return (await axios.get(this.getVersion_url(), null || undefined))
            .data as VersionInfo;
    }
}
