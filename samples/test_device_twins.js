'use strict';
var Client = require('azure-iot-device').Client;
var Protocol = require('azure-iot-device-mqtt').Mqtt;
const chalk = require('chalk');

var connectionString = 'HostName=IoTHub-SAJ.azure-devices.net;DeviceId=IoT2;SharedAccessKey=lNXRziVOfTOwEmxyUbzvFJ0Cf7p3+U0hYLlDrh/wfAM=';
var client = Client.fromConnectionString(connectionString, Protocol);

var initConfigChange = function(twin, patch) {
    twin.properties.reported.update(patch, function(err) {
        if (err) {
            console.log(chalk.red('Could not report properties'));
        } else {
            console.log(chalk.green('Reported pending config change: ' + JSON.stringify(patch)));
            setTimeout(function() {completeConfigChange(twin, patch);}, 10000);
        }
    });
}

var completeConfigChange =  function(twin, patch) {
    if (patch.settings) {
        var settingsBase = twin.properties.reported.settings;
        settingsBase = settingsBase.pendingConfig;
        settingsBase.status = "Success";
        delete settingsBase.pendingConfig;
        var patch = {
            settings: settingsBase
        };    
        patch.settings.pendingConfig = null;
    }

    twin.properties.reported.update(patch, function(err) {
        if (err) {
            console.error(chalk.red('Error reporting properties: ' + err));
        } else {
            console.log(chalk.green('Reported successful config change: ' + JSON.stringify(patch)));
        }
    });

}

client.open(function(err) {
    if (err) {
        console.error(chalk.red('Could not open IotHub client'));
    } else {
        client.getTwin(function(err, twin) {
            if (err) {
                console.error(chalk.red('Could not get device twin'));
            } else {
                console.log(chalk.blue('Retrieved device twin'));
                twin.on('properties.desired', function(desiredChange) {
                    if (desiredChange.settings) {
                        console.log(chalk.green("Received desired settings: "+JSON.stringify(desiredChange)));
                        if (twin.properties.reported.settings) {
                            var currentsettings = twin.properties.reported.settings
                        } else {
                            var currentsettings = {};
                        }
                        currentsettings.pendingConfig = twin.properties.desired.settings;
                        currentsettings.status = "Pending";
                        console.log(chalk.greenBright("Pending Settings: "+JSON.stringify(currentsettings)));
                        var patch = {
                            settings: currentsettings
                        };
                        initConfigChange(twin, patch);
                    } else {
                        console.log(chalk.yellow("No desired settings to process!"));
                        process.exit()
                    }

                });
            }
        });
    }
});