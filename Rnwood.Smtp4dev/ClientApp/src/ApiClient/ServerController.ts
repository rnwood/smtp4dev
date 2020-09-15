

import Server from './Server';
import axios from "axios";

export default class ServerController {
               
    constructor(){
    }
        
    
    
    // get: api/Server  
    public getServer_url(): string {
        return `api/Server`;
    }

    public async getServer(): Promise<Server> {

        return (await axios.get(this.getServer_url(), null || undefined)).data as Server;
    }
    
    // post: api/Server  
    public updateServer_url(): string {
        return `api/Server`;
    }

    public async updateServer(serverUpdate: Server): Promise<void> {

        return (await axios.post(this.updateServer_url(), serverUpdate || undefined)).data as void;
    }
}