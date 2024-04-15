<template>
  <div>Version: <span v-text="version"></span></div>
</template>

<script lang="ts">

    import { Component, Vue, Prop, toNative } from 'vue-facing-decorator';
import Info from "../ApiClient/VersionInfo";
import VersionController from "../ApiClient/VersionController";

@Component({})
class Version extends Vue {
  info: Info | null = null;

  async mounted() {
    this.info = await new VersionController().getVersion();
  }

  get version() {
    return this.info?.infoVersion ?? "";
  }
    }

    export default toNative(Version)
</script>