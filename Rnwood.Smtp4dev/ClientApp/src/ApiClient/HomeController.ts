 

import IActionResult from './IActionResult';
import axios from "axios";

export default class HomeController {
               
    constructor(){
    }
        
    
    
    // post: api/Home/  
    public index_url(): string {
        return `api/Home/`;
    }

    public async index(): Promise<IActionResult> {

        return (await axios.post(this.index_url(), null || undefined)).data as IActionResult;
    }
    
    // post: api/Home/  
    public error_url(): string {
        return `api/Home/`;
    }

    public async error(): Promise<IActionResult> {

        return (await axios.post(this.error_url(), null || undefined)).data as IActionResult;
    }
}