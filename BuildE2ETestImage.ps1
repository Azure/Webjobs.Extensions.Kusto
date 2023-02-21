function BuildE2ETestImage {
    <#
    .SYNOPSIS
        Re-builds base images for azure functions (based on images from https://github.com/Azure/azure-functions-docker)
    .DESCRIPTION
        Re-builds base images for azure functions. On the docker image created, the sample functions can then be copied and run. Can be used for End-To-End tests for all multilang samples.
    .NOTES
        On visual studio, the function can be built by opening the file and using the F8 key
    .EXAMPLE
        To build the node sample use the following : 
            BuildE2ETestImage 
        Extension bundle path is an optional parameter can be used. This is handy if the extension bundle has to be built locally and a custom extension is created for the first time and not available on the bundle.
        Refer : https://github.com/Azure/azure-functions-extension-bundles  on how to build the bundle on Linux/Windows
            BuildE2ETestImage -ExtensionBundlePath "C:\Functions\azure-functions-extension-bundles\artifacts\Microsoft.Azure.Functions.ExtensionBundle.Preview.4.5.0_any-any.zip" -Acr <acr>.azurecr.io -DockerPush $true
    #>
    Param(
        [Parameter(Mandatory = $false, HelpMessage = "If building bundles locally and using them for testing, use this parameter. Build instructions on https://github.com/Azure/azure-functions-extension-bundles. e.g.The path is the path azure-functions-extension-bundles\artifacts\Microsoft.Azure.Functions.ExtensionBundle.Preview.4.5.0_any-any.zip")][AllowNull()][string]$ExtensionBundlePath = $null,
        [Parameter(Mandatory = $false, HelpMessage = "The azure container registry to push to myacr.azure.io")][AllowNull()][string]$Acr = $null,
        [Parameter(Mandatory = $false, HelpMessage = "If the tagged image needs to be pushed to the docker hub")][bool]$DockerPush = $false
    ) #end param
    $BaseImageTag = "node:4-node16-core-tools" # Of all images this seems to be the best so far
    $TargetImageName="func-az-kusto-base" # Name of the target image
    $BuildDate = (Get-Date).ToString("yyyyMMdd")
    $TargetFileLocation="./samples/docker"
    $TargetDockerFile="${TargetFileLocation}\DockerFile"
    Write-Host "------------------------------------------------------------------------------------------------------------------------------"
    Write-Host "Using base image mcr.microsoft.com/azure-functions/${BaseImageTag}" -ForegroundColor Green
    # build the DLL so that we can use this when we do the rebuild
    Write-Host "------------------------------------------------------------------------------------------------------------------------------"
    Write-Host "Cleaning and building Project" -ForegroundColor Green
    dotnet clean
    dotnet build /p:Configuration=Release
    # The docker file goes to resources folder
    Write-Host "------------------------------------------------------------------------------------------------------------------------------"
    #If a locally built extension bundle is specified , add it
    #If an override on the ExtensionBundlePath is specified then use this in the image on the docker file.    
    if (!$ExtensionBundlePath) {
        Write-Host "Creating docker file to build with tag '${TargetImageName}:$BuildDate'" -ForegroundColor Green
        ((Get-Content -path .\samples\docker\Docker-template.dockerfile -Raw) -creplace 'imagename', "mcr.microsoft.com/azure-functions/${BaseImageTag}" -creplace 'bundlepath', $ExtensionBundlePath) | Set-Content -Path $TargetDockerFile
    }
    else {
        $EscapedPath = (Get-Item $ExtensionBundlePath | Resolve-Path -Relative) -replace "\\", "/"
        Copy-Item -Path $EscapedPath $TargetFileLocation/Microsoft.Azure.Functions.ExtensionBundle.zip
        Write-Host "Creating docker file to build with tag '${TargetFileLocation}:$BuildDate' with extension bundle copy on path $EscapedPath" -ForegroundColor Green
        ((Get-Content -path .\samples\docker\Docker-template.dockerfile -Raw) -creplace 'imagename', "mcr.microsoft.com/azure-functions/${BaseImageTag}" -creplace 'bundlepath', $EscapedPath -replace '#COPY', "COPY ${TargetFileLocation}/Microsoft.Azure.Functions.ExtensionBundle.zip ") | Set-Content -Path $TargetDockerFile
    }
    if(!$Acr)
    {
        $TagCreated = "${TargetImageName}:${BuildDate}"
        $LatestTagCreated = "$Acr/${TargetImageName}:latest"
        Write-Host "Creating docker tag $TagCreated and $LatestTagCreated " -ForegroundColor Green
        docker build -t "${TargetImageName}-$TagCreated" -t "${TargetImageName}:latest" -f $TargetDockerFile .
    }
    else {
        $TagCreated = "$Acr/${TargetImageName}:$BuildDate"
        $LatestTagCreated = "$Acr/${TargetImageName}:latest"
        Write-Host "Creating ACR docker tag $TagCreated and $LatestTagCreated " -ForegroundColor Green
        docker build -t $TagCreated -t $LatestTagCreated -f $TargetDockerFile .
        if($true -eq $DockerPush)
        {
            docker image push --all-tags "$Acr/${TargetImageName}"
        }
    }
    
    if ($?) {
        Write-Host "Image build '${TargetImageName}:${BuildDate}' complete " -ForegroundColor Green
    }
    else {
        Write-Host "Image build '${TargetImageName}:${BuildDate}'  failed. Is docker running  or are there any errors reported in execution of this script ?" -ForegroundColor Red
    }
    Remove-Item  $TargetFileLocation/Microsoft.Azure.Functions.ExtensionBundle.zip -ErrorAction Ignore
}