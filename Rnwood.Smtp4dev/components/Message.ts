module Api {

	 
		export class Message { 
			id: string;
			from: string;
			to: string;
			receivedDate: Date;
			subject: string;
			parts: MessagePart[];
		}

}

