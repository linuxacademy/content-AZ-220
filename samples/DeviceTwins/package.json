'use strict';

var Client = require('azure-iot-device').Client;
var Protocol = require('azure-iot-device-mqtt').Mqtt;

var connectionString = 'HostName=IoTHub-SAJ.azure-devices.net;DeviceId=IoTTwin;SharedAccessKey=7llQ8o398j61uLhqjVyjk7dDXnMJdy6SP5q3oJD8Ysg=';
var client = Client.fromConnectionString(connectionString, Protocol);

var initConfigChange = function(twin, patch) {
    twin.properties.reported.update(patch, function(err) {
        if (err) {
            console.log('Could not report properties');
        } else {
            console.log('Reported pending config change: ' + JSON.stringify(patch));
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
            console.error('Error reporting properties: ' + err);
        } else {
            console.log('Reported successful config change: ' + JSON.stringify(patch));
        }
    });

}

client.open(function(err) {
    if (err) {
        console.error('Could not open IotHub client');
    } else {
        client.getTwin(function(err, twin) {
            if (err) {
                console.error('Could not get device twin');
            } else {
                console.log('Retrieved device twin');
                twin.on('properties.desired', function(desiredChange) {
                    if (desiredChange.settings) {
                        console.log("Received desired settings: "+JSON.stringify(desiredChange));
                        if (twin.properties.reported.settings) {
                            var currentsettings = twin.properties.reported.settings
                        } else {
                            var currentsettings = {};
                        }
                        currentsettings.pendingConfig = twin.properties.desired.settings;
                        currentsettings.status = "Pending";
                        console.log("Pending Settings: "+JSON.stringify(currentsettings));
                        var patch = {
                            settings: currentsettings
                        };
                        initConfigChange(twin, patch);
                    } else {
                        console.log("No desired settings to process!");
                        process.exit()
                    }

                });
            }
        });
    }
});
