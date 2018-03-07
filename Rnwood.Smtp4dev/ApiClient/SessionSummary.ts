 

export default class SessionSummary {

    constructor(clientAddress: string, clientName: string, numberOfMessages: number, id: string, endDate: Date, ) {
         
        this.clientAddress = clientAddress; 
        this.clientName = clientName; 
        this.numberOfMessages = numberOfMessages; 
        this.id = id; 
        this.endDate = endDate;
    }

     
    clientAddress: string; 
    clientName: string; 
    numberOfMessages: number; 
    id: string; 
    endDate: Date;
}
