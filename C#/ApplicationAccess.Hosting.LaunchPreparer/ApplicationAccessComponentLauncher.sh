#!/bin/bash

#
# Copyright 2023 Alastair Wyse (https://github.com/alastairwyse/ApplicationAccess/)
# 
# Licensed under the Apache License, Version 2.0 (the "License");
# you may not use this file except in compliance with the License.
# You may obtain a copy of the License at
# 
#     http://www.apache.org/licenses/LICENSE-2.0
# 
# Unless required by applicable law or agreed to in writing, software
# distributed under the License is distributed on an "AS IS" BASIS,
# WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
# See the License for the specific language governing permissions and
# limitations under the License.
#
# ------------------------------------------------------------------------------
# Script: ApplicationAccessComponentLauncher.sh
# Description: Prepares to start and starts an ApplicationAccess hosted component.
#   Calls the 'LaunchPreparer' utility to validate start parmaeters, and setup
#   configuration for TCP port to listen on, log level, and component settings.
#   Designed to be run as the docker 'ENTRYPOINT' when hosting the component 
#   through a docker container.
#
# Arguments:
#   $1 - The ApplicationAccess component to launch.  Should be one of 
#   'EventCacheNode', 'ReaderNode', 'ReaderWriterNode', or 'WriterNode'.  Not 
#   required in 'EncodeConfiguration' mode.
#
# Usage Example:
#   ./ApplicationAccessComponentLauncher.sh ReaderNode
#
# Required Environment Variables:
#   These must be setup via the 'export' statement if running through Linux, or 
#   via the -e parameter if passed to a docker container.
#   For 'Launch' mode:
#     MODE - e.g. 'MODE=Launch'
#     LISTEN_PORT - e.g 'LISTEN_PORT=5000'
#     MINIMUM_LOG_LEVEL - e.g 'MINIMUM_LOG_LEVEL=Critical'
#     ENCODED_JSON_CONFIGURATION - e.g. 'ENCODED_JSON_CONFIGURATION=H4sIAAAAAAACCqpWUApLzClNVbJSUPIsVvAK9vdTUqgFAAAA//8='
#   For 'EncodeConfiguration' mode:
#     MODE - e.g 'MODE=EncodeConfiguration'
#     CONFIGURATION_FILE_PATH - e.g 'CONFIGURATION_FILE_PATH=appsettings.Production.json'
#
# Example 'docker run' commands when configured as a docker container 'ENTRYPOINT'
#   'Launch' mode:
#     docker run -it --rm -p 5000:5000 -e MODE=Launch -e LISTEN_PORT=5000 -e MINIMUM_LOG_LEVEL=Warning -e ENCODED_JSON_CONFIGURATION=H4sIAAAAAAACCqpWUApLzClNVbJSUPIsVvAK9vdTUqgFAAAA//8= --name appaccess_readerwriter 792f8ef28edf
#   'EncodeConfiguration' mode:
#     docker run -it --rm -v /home/TestUser/AppAccessDocker/ReaderWriterSource:/ext -e MODE=EncodeConfiguration -e CONFIGURATION_FILE_PATH=/ext/appsettings.Production.json --name appaccess_readerwriter b306fbdce9e5

if [ $MODE == EncodeConfiguration ]
then
    # Run LaunchPreparer in 'EncodeConfiguration' mode (encoded config will be written to the console)
    dotnet ApplicationAccess.Hosting.LaunchPreparer.dll -mode $MODE -configurationFilePath $CONFIGURATION_FILE_PATH
else
    # Validate inputs and prepare configuration
    launchPreparerOutput=` dotnet ApplicationAccess.Hosting.LaunchPreparer.dll -mode $MODE -component $1 -listenPort $LISTEN_PORT -minimumLogLevel $MINIMUM_LOG_LEVEL -encodedJsonConfiguration $ENCODED_JSON_CONFIGURATION `

    # '$?' in bash gets the return value of the most recently execution
    if [ $? -eq 0 ]
    then
        # Execution of LaunchPreparer was successful, so start the ApplicationAccess component ($launchPreparerOutput contains the name of the dll corresponding to the specified component)
        dotnet $launchPreparerOutput --urls http://*:$LISTEN_PORT --environment Production
    else
        # execution of LaunchPreparer was not successful, so show its output (error message and usage info) on the console
        echo "$launchPreparerOutput"
    fi
fi
