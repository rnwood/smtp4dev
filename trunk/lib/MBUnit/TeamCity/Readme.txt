Gallio.TeamCityIntegration
==========================

This plugin provides a Test Runner Extension that publishes "service messages" that
TeamCity can interpret and present in its test results.

Beginning with Gallio v3.0.7, Gallio will automatically 

Prior to Gallio v3.0.7, it was necessary to specify the TeamCity integration extension
manually as an argument to the test runner.  Now Gallio will automatically detect
that it is running within the context of a TeamCity build by checking for the exitents
of the "TEAMCITY_VERSION" environment variable which is predefined by TeamCity.


>>> IF THE AUTO-DETECTION FAILS TO WORK, HERE ARE THE OLD MANUAL INSTRUCTIONS <<<

Only use these instructions if for some reason the auto-detection appears to fail.

Set the RunnerExtensions argument of the Gallio test runner you are using to
    "TeamCityExtension,Gallio.TeamCityIntegration"

Examples:
  
If you are using the Gallio MSBuild task:
    <Gallio RunnerExtensions="TeamCityExtension,Gallio.TeamCityIntegration"
        ... other arguments... />

If you are using the Gallio NAnt task:
    <gallio ... other arguments>
      <runner-extension value="TeamCityExtension,Gallio.TeamCityIntegration" />
      ... other arguments
    </gallio>

If you are using the Gallio Echo task:
    Gallio.Echo /re:TeamCityExtension,Gallio.TeamCityIntegration ... other arguments...
