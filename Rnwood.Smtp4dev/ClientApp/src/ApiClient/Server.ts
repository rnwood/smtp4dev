

export default class Server {
 
    constructor(isRunning: boolean, exception: string, portNumber: number, ) {
         
        this.isRunning = isRunning; 
        this.exception = exception; 
        this.portNumber = portNumber;
    }

     
    isRunning: boolean; 
    exception: string; 
    portNumber: number;
}
