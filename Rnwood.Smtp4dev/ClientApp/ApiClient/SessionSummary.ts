 

export default class SessionSummary {

    constructor(clientAddress: string, clientName: string, numberOfMessages: number, id: string, endDate: Date, startDate: Date, terminatedWithError: boolean, size: number, ) {
         
        this.clientAddress = clientAddress; 
        this.clientName = clientName; 
        this.numberOfMessages = numberOfMessages; 
        this.id = id; 
        this.endDate = endDate; 
        this.startDate = startDate; 
        this.terminatedWithError = terminatedWithError; 
        this.size = size;
    }

     
    clientAddress: string; 
    clientName: string; 
    numberOfMessages: number; 
    id: string; 
    endDate: Date; 
    startDate: Date; 
    terminatedWithError: boolean; 
    size: number;
}
