import ClientSettings from "./ClientSettings";
import axios from "axios";

export default class ClientSettingsController {
    constructor() {}

    public getClientSettings_url(): string {
        return `api/clientsettings`;
    }

    public async getClientSettings(): Promise<ClientSettings> {
        return (await axios.get(this.getClientSettings_url())).data as ClientSettings;
    }
}
