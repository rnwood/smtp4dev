import SessionWarning from "./SessionWarning";


export default class Session {
 
    constructor(id: string, errorType: string, error: string, warnings: SessionWarning[]) {
         
        this.id = id; 
        this.errorType = errorType; 
        this.error = error;
        this.warnings = warnings;
    }

     
    id: string; 
    errorType: string; 
    error: string;
    warnings: SessionWarning[];
}
