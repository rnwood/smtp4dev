<template>
  <div>Version: <span v-text="version"></span></div>
</template>

<script lang="ts">
import Vue from "vue";
import Component from "vue-class-component";
import Info from "../ApiClient/VersionInfo";
import VersionController from "../ApiClient/VersionController";

@Component({})
export default class Version extends Vue {
  info: Info | null = null;

  async mounted() {
    this.info = await new VersionController().getVersion();
  }

  get version() {
    return this.info?.infoVersion ?? "";
  }
}
</script>