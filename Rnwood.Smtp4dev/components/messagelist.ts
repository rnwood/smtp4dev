import Vue from 'vue'
import Component from 'vue-class-component'
import axios from 'axios';

@Component({
    template: `
        <div class="messagelist">
            <button v-on:click="refresh">Refresh</button>
            <button v-on:click="clear">Clear</button>

            <el-table ref="singleTable" :data="messages" highlight-current-row>
                <el-table-column type="index" width="50">
                </el-table-column>
                <el-table-column property="receivedDate" label="Received" width="120">
                </el-table-column>
                <el-table-column property="from" label="From" width="120">
                </el-table-column>
                <el-table-column property="to" label="To">
                </el-table-column>
            </el-table>
        </div>
`
})
export default class MessageList extends Vue {
    messages: any[] = []
    errors: Error[] = []

    clear(): void {
        axios.delete("/api/messages/*")
            .then(response => {
                this.refresh();
            })
            .catch(e => {
                this.errors.push(e)
            })
    }

    refresh(): void {

        axios.get("/api/messages")
            .then(response => {
                this.messages = response.data
                this.messages.forEach(m => m.content = atob(m.data));
            })
            .catch(e => {
                this.errors.push(e)
            })

    }

    created(): void {

        this.refresh();

    }
}