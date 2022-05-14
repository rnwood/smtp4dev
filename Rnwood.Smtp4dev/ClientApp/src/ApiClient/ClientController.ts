import Client from "./Client";
import axios from "axios";

export default class ClientController {
    constructor() {}

    public getClient_url(): string {
        return `api/client`;
    }

    public async getClient(): Promise<Client> {
        return (await axios.get(this.getClient_url())).data as Client;
    }
}
