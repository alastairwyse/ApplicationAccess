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

#
# NAME
#     Docker-Build
#
# SYNOPSIS
#     Builds a hosted ApplicationAccess component, the LaunchPreparer tool, and scripts
#     to host the component in a Docker container, into a folder.
#
# SYNTAX
#     Docker-Build [-Component] <String> [-OutputFolder] <String> [-TarFileName] <String> [-IncludePdbFiles] <Switch>
#
# EXAMPLES
#     .\Docker-Build.ps1 "EventCache" "C:\Temp\DockerBuild\EventCache\" "EventCache.tar" -IncludePdbFiles
#     .\Docker-Build.ps1 "ReaderWriter" "C:\Temp\DockerBuild\ReaderWriter\" "ReaderWriter.tar"
#     .\Docker-Build.ps1 "ReaderWriterLite" "C:\Temp\DockerBuild\ReaderWriterLite\" "ReaderWriterLite.tar" -IncludePdbFiles
#     .\Docker-Build.ps1 "DependencyFreeReaderWriter" "C:\Temp\DockerBuild\DependencyFreeReaderWriter\" "DependencyFreeReaderWriter.tar"
#     .\Docker-Build.ps1 "Reader" "C:\Temp\DockerBuild\Reader\" "Reader.tar" -IncludePdbFiles
#     .\Docker-Build.ps1 "Writer" "C:\Temp\DockerBuild\Writer\" "Writer.tar"
#     .\Docker-Build.ps1 "DistributedReader" "C:\Temp\DockerBuild\DistributedReader\" "DistributedReader.tar" -IncludePdbFiles
#     .\Docker-Build.ps1 "DistributedWriter" "C:\Temp\DockerBuild\DistributedWriter\" "DistributedWriter.tar"
#     .\Docker-Build.ps1 "DistributedOperationCoordinator" "C:\Temp\DockerBuild\DistributedOperationCoordinator\" "DistributedOperationCoordinator.tar"
#
# NOTES / TODO
#     Currently the below command must be executed to allow the scripts to be run (without 
#     it running results in error '[script name] is not digitally signed. You cannot run 
#     this script on the current system.')...
#       'Set-ExecutionPolicy -ExecutionPolicy Bypass -Scope CurrentUser'
#     This command should subsequently reverse that security bypass...
#       'Set-ExecutionPolicy -ExecutionPolicy Undefined -Scope CurrentUser'
#
#     Resulting tar file can be extracted on the unix-side with the following command...
#       tar -xf [tar filename]
#

# Read and validate input parameters
Param (
[Parameter(Position=0, Mandatory=$True, HelpMessage="Enter the component to build (''Reader'', ''Writer'', ''ReaderWriter'', ''ReaderWriterLite'', ''EventCache'', ''DependencyFreeReaderWriter'', ''DistributedReader'', or ''DistributedWriter'')")]
[ValidateNotNullorEmpty()]
[string]$Component,
[Parameter(Position=1, Mandatory=$True, HelpMessage="Enter the build destination folder")]
[ValidateNotNullorEmpty()]
[string]$OutputFolder,
[Parameter(Position=2, Mandatory=$True, HelpMessage="Enter the name of the tar archive to write the component's files' to")]
[ValidateNotNullorEmpty()]
[string]$TarFileName,
[Parameter(Position=3, Mandatory=$False, HelpMessage="Include this parameter if .pdb files should be included in the build files")]
[switch]$IncludePdbFiles=$False
)

# Constants
$unixScriptFiles = @("ApplicationAccessComponentLauncher.sh", "Dockerfile")

# Map the component to the relative location of the source code for that component
if ($Component -eq 'Reader') {
    $componentPath = '..\ApplicationAccess.Hosting.Rest.Reader'
}
elseif ($Component -eq 'ReaderWriter') {
    $componentPath = '..\ApplicationAccess.Hosting.Rest.ReaderWriter'
}
elseif ($Component -eq 'ReaderWriterLite') {
    $componentPath = '..\ApplicationAccess.Hosting.Rest.ReaderWriterLite'
}
elseif ($Component -eq 'Writer') {
    $componentPath = '..\ApplicationAccess.Hosting.Rest.Writer'
}
elseif ($Component -eq 'EventCache') {
    $componentPath = '..\ApplicationAccess.Hosting.Rest.EventCache'
}
elseif ($Component -eq 'DependencyFreeReaderWriter') {
    $componentPath = '..\ApplicationAccess.Hosting.Rest.DependencyFreeReaderWriter'
}
elseif ($Component -eq 'DistributedReader') {
    $componentPath = '..\ApplicationAccess.Hosting.Rest.DistributedReader'
}
elseif ($Component -eq 'DistributedWriter') {
    $componentPath = '..\ApplicationAccess.Hosting.Rest.DistributedWriter'
}
elseif ($Component -eq 'DistributedOperationCoordinator') {
    $componentPath = '..\ApplicationAccess.Hosting.Rest.DistributedOperationCoordinator'
}
else {
    throw "Argument 'Component' contains invalid value '$($Component)'"
}

# Capture the current directory
$initialDirectory = Get-Location

# Create the output folder
try {
    New-Item -Path $OutputFolder -ItemType Directory -Force
    Remove-Item (Join-Path -Path $OutputFolder -ChildPath '*') -Recurse
}
catch {
    throw "Failed to create and clean output folder '$($OutputFolder)'"
}

# Move to the component Directory
cd $componentPath

# Build the component
dotnet build -c Release -o $OutputFolder

# Remove any unnecessary appsettings files from the build folder
$appsettingsFiles = Get-ChildItem -Path $OutputFolder -Filter 'appsettings.*.json'
foreach ($currentAppsettingsFile in $appsettingsFiles) {
    $currentAppsettingsFilePath = Join-Path -Path $OutputFolder -ChildPath $currentAppsettingsFile
    Remove-Item $currentAppsettingsFilePath
}

# Build the LaunchPreparer tool
cd ..\ApplicationAccess.Hosting.LaunchPreparer\
dotnet build -c Release -o $OutputFolder 

# Remove .pdb files
if ($IncludePdbFiles -eq $false) {
    $pdbFiles = Get-ChildItem -Path $OutputFolder -Filter '*.pdb'
    foreach ($currentPdbFile in $pdbFiles) {
        $currentPdbFilePath = Join-Path -Path $OutputFolder -ChildPath $currentPdbFile
        Remove-Item $currentPdbFilePath
    }
}

# Set unix newlines in any script files used in the container
foreach ($currentUnixScriptFile in $unixScriptFiles) {
    $currentScriptFilePath = Join-Path -Path $OutputFolder -ChildPath $currentUnixScriptFile
    $currentScriptFileContent = Get-Content -Path $currentScriptFilePath # -Raw
    $currentScriptFileContent = $currentScriptFileContent -Join "`n"
    Set-Content -Path $currentScriptFilePath -Value $currentScriptFileContent -Force -NoNewline
}

# Store the output folder into a tar archive
$tarFilePath = Join-Path -Path $OutputFolder -ChildPath $TarFileName
tar -C $OutputFolder --exclude $TarFileName -cf $tarFilePath *.*

# Return to the original directory
cd $initialDirectory

Write-host "SUCCESS: Completed Docker build process."