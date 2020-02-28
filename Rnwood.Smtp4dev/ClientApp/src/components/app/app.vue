<template>

    <div id='app-root' v-cloak class="vfillpanel">
        <router-view></router-view>
    </div>

</template>

<script lang="ts">
    import Vue from 'vue';
    import { Component } from 'vue-property-decorator';
    import 'element-theme-default/lib/index.css';

    @Component({})
    export default class AppComponent extends Vue {
    }

</script>
<style lang="scss">
// Import Font Awesome 5 Free
$fa-css-prefix: 'el-icon-fa';
$fa-font-path: '~@fortawesome/fontawesome-free/webfonts';

@import '~@fortawesome/fontawesome-free/scss/fontawesome.scss';
@import '~@fortawesome/fontawesome-free/scss/solid.scss';
@import '~@fortawesome/fontawesome-free/scss/brands.scss';

// Override Element UI's icon font
.fas {
  font-family: 'Font Awesome 5 Free' !important;
}

.fab {
  font-family: 'Font Awesome 5 Brands' !important;
}
</style>
