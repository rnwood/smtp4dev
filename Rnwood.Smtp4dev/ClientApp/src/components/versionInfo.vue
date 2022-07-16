<template>
  <div>Version: <span v-text="version"></span></div>
</template>

<script lang="ts">
import Vue from "vue";
import Component from "vue-class-component";
import Info from "../ApiClient/Info";
import InfoController from "../ApiClient/InfoController";

@Component({})
export default class Version extends Vue {
  info: Info | null = null;

  async mounted() {
    this.info = await new InfoController().getInfo();
  }

  get version() {
    return this.info?.infoVersion ?? "";
  }
}
</script>