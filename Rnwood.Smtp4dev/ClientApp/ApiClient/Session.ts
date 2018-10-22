 

export default class Session {

    constructor(id: string, errorType: string, error: string, ) {
         
        this.id = id; 
        this.errorType = errorType; 
        this.error = error;
    }

     
    id: string; 
    errorType: string; 
    error: string;
}
