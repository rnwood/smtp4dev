import messagelistpager from '../messagelistpager.vue';
import { createLocalVue, mount, Wrapper } from '@vue/test-utils';
import Vue from 'vue';
import ElementUI from "element-ui";

Vue.use(ElementUI);

describe('component: messagelistpager', ()=> {
    const localVue = createLocalVue();
    let wrapper: Wrapper<Vue>;

    beforeEach(()=> {
        wrapper = mount(messagelistpager);
    })

    it('compiles', (): void => {
        const x = wrapper.html();
        expect(x).not.toBeNull();
    })
});
