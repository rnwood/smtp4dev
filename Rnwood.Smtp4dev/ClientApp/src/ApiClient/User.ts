
export default class User {
    username: string;
    password: string;
    defaultMailbox: string;
 
    constructor(username: string, password: string, defaultMailbox: string) {
         
        this.username = username;
        this.password = password;
        this.defaultMailbox = defaultMailbox;
    }


}
