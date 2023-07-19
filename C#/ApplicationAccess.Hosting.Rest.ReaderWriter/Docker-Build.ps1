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
# NAME
#     Docker-Build
#
# SYNOPSIS
#     Builds a hosted ApplicationAccess component, the LaunchPreparer tool, and scripts
#     to host the component in a Docker container, into a folder.
#
# SYNTAX
#     Docker-Build [-OutputFolder] <String> [-ZipFileName] <String>
#
# ** TODO **
#     Compress is putting the test folder into the zip, rather than all the files in the folder
#     Usn't unzipping on the unix side properly... runtimes folder comes up as access denied??
#
#     ATM need to run the below command to allow the scripts to be run (without it running 
#     results in error '[script name] is not digitally signed. You cannot run this script on the current 
#     system.')...
#       'Set-ExecutionPolicy -ExecutionPolicy Bypass -Scope CurrentUser'
#     This command should subsequently reverse that security bypass...
#       'Set-ExecutionPolicy -ExecutionPolicy Undefined -Scope CurrentUser'
#
#     Add some 'Write-Host' statements to tell the user what's being done at each step
#

# Read and validate input parameters
Param (
[Parameter(Position=0, Mandatory=$True, HelpMessage="Enter the build folder")]
[ValidateNotNullorEmpty()]
[string]$outputFolder,
[Parameter(Position=1 ,Mandatory=$True, HelpMessage="Enter the name of the zip archive to write the component to")]
[ValidateNotNullorEmpty()]
[string]$zipFileName
)

# Create the output folder
try {
	New-Item -Path $outputFolder -ItemType Directory -Force
	Remove-Item $outputFolder -Recurse
}
catch {
	Write-Host 'Failed to create and clean output folder ''$($outputFolder)'''
}

# Build the current component
dotnet build -c Release -o $outputFolder

# Remove any unnecessary appsettings files from the build folder
$appsettingsFiles = Get-ChildItem -Path $outputFolder -Filter 'appsettings.*.json'
foreach ($currentAppsettingsFile in $appsettingsFiles){
	$currentAppsettingsFilePath = Join-Path -Path $outputFolder -ChildPath $currentAppsettingsFile
    Remove-Item $currentAppsettingsFilePath
}

# Build the LaunchPreparer tool
cd ..\ApplicationAccess.Hosting.LaunchPreparer\
dotnet build -c Release -o $outputFolder 

# Compress the output folder into a zip archive
$zipFilePath = Join-Path -Path $outputFolder -ChildPath $zipFileName
Compress-Archive -Path $outputFolder -DestinationPath $zipFilePath -CompressionLevel Optimal