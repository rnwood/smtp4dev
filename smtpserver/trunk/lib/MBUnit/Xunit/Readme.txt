xUnit.net Adapter Plugin
========================

This plugin uses the xUnit.net test runner to adapt xUnit.net tests so that
they can run within Gallio and be manipulated by Gallio-based tools.

The plugin assembly is deliberately NOT signed using a strong name.
You can replace the underlying test framework with newer versions as
long as they are binary compatible with the originally distributed version.
