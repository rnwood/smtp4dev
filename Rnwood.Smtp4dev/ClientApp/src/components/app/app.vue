<template>
  <div id="app-root" v-cloak class="vfillpanel">
    <transition name="slide-fade" mode="out-in">
      <router-view></router-view>
    </transition>
  </div>
</template>

<script lang="ts">
import Vue from "vue";
import { Component } from "vue-property-decorator";
import '@fortawesome/fontawesome-free/css/all.css'
import '@fortawesome/fontawesome-free/js/all.js'

@Component({})
export default class AppComponent extends Vue {}
</script>
<style lang="scss">
// Import Font Awesome 5 Free
$fa-css-prefix: "el-icon-fa";
$fa-font-path: "~@fortawesome/fontawesome-free/webfonts";

$__color-primary: #000047ff;
$--font-path: "~element-ui/lib/theme-chalk/fonts";
$--button-small-padding-vertical: 7px;
$--button-small-padding-horizontal: 12px;
$--input-small-padding-vertical: 7px;

@import "~element-ui/packages/theme-chalk/src/index";
</style>
