<template>
    <div id="app-root" v-cloak class="vfillpanel">

        <router-view></router-view>

    </div>
</template>

<script lang="ts">
    import { Component, Vue, toNative } from "vue-facing-decorator";
    import '@fortawesome/fontawesome-free/css/all.css'
    import '@fortawesome/fontawesome-free/js/all.js'

    @Component
    class LocalApp extends Vue {

    }

    export default toNative(LocalApp);
</script>
<style lang="scss">
    // Import Font Awesome 5 Free
    $fa-css-prefix: "fa";
    $fa-font-path: "~@fortawesome/fontawesome-free/webfonts";

    $__color-primary: #000047ff;
    $--font-path: "~element-plus/theme-chalk/fonts";
    $--button-small-padding-vertical: 7px;
    $--button-small-padding-horizontal: 12px;
    $--input-small-padding-vertical: 7px;

    @import "element-plus/theme-chalk/src/index";
</style>
