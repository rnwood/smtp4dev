import Vue from 'vue'
import Component from 'vue-class-component'
import axios from 'axios';
import { HubConnection } from '@aspnet/signalr-client'

@Component({
    template: `
        <div class="messagelist">
            <button v-on:click="refresh">Refresh</button>
            <button v-on:click="clear">Clear</button>

            <div v-if="error">{{error.message}}</div>
            <el-table ref="singleTable" :data="messages" highlight-current-row>
                <el-table-column type="index" width="50">
                </el-table-column>
                <el-table-column property="receivedDate" label="Received" width="120">
                </el-table-column>
                <el-table-column property="from" label="From" width="120">
                </el-table-column>
                <el-table-column property="to" label="To" width="120">
                </el-table-column>
                <el-table-column property="subject" label="Subject">
                </el-table-column>
            </el-table>
        </div>
`
})
export default class MessageList extends Vue {
    messages: any[] = []
    error: Error = null

    private connection = new HubConnection('/hubs/messages');
 
    clear(): void {
        axios.delete("/api/messages/*")
            .then(response => {
                this.refresh();
            })
            .catch(e => {
                this.error = e
            })
    }

    refresh(): void {

        this.error = null;

        axios.get("/api/messages")
            .then(response => {
                this.messages = response.data;
            })
            .catch(e => {
                this.error = e;
            })

    }

    async created() {

        this.connection.on('messageadded', data => {
            this.refresh();
        });

        this.connection.on('messageremoved', data => {
            this.refresh();
        });

        await this.connection.start();

        this.refresh();

    }
}