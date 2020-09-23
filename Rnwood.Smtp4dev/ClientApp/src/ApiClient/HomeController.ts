


import axios from "axios";

export default class HomeController {
               
    constructor(){
    }
        
    
    
    // post: api/Home/  
    public index_url(): string {
        return `api/Home/`;
    }

    public async index(): Promise<void> {

        return (await axios.post(this.index_url(), null || undefined)).data as void;
    }
    
    // post: api/Home/  
    public error_url(): string {
        return `api/Home/`;
    }

    public async error(): Promise<void> {

        return (await axios.post(this.error_url(), null || undefined)).data as void;
    }
}