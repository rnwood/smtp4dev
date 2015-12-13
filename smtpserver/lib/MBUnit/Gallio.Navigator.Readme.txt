Gallio.Navigator
================

The Gallio Navigator component enables external applications to navigate to source code
by clicking on links that are interpreted by a Pluggable Protocol Handler or by loading
an ActiveX / COM object marked safe for scripting.

These services are intended to present a minimum security risk and specifically do not
disclose user information to the calling application.

(In the future this mechanism may be used to provide additional Gallio services.)

NavigateTo Service:

  Link Format: gallio:navigateTo?path=<path>&line=<lineNumber>&column=<columnNumber>
  
  ActiveX:     Gallio.Navigator.GallioNavigator class
               bool NavigateTo(string path, int lineNumber, int columnNumber)

  Parameters:

    <path>         - The path of the source file.
    <lineNumber>   - The 1-based line number, or 0 if unspecified.
    <columnNumber> - The 1-based column number, or 0 if unspecified.
