function ReBuildSampleImages {
    <#
    .SYNOPSIS
        Re-builds base images for azure functions (based on images from https://github.com/Azure/azure-functions-docker)
    .DESCRIPTION
        Re-builds base images for azure functions. On the docker image created, the sample functions can then be copied and run. Can be used for End-To-End tests for all multilang samples.
    .NOTES
        On visual studio, the function can be built by opening the file and using the F8 key
    .EXAMPLE
        To build the node sample use the following : 
            ReBuildSampleImages -Language node -BaseImagePath 4-node16-core-tools
        Extension bundle path is an optional parameter can be used. This is handy if the extension bundle has to be built locally and a custom extension is created for the first time and not available on the bundle.
        Refer : https://github.com/Azure/azure-functions-extension-bundles  on how to build the bundle on Linux/Windows
            ReBuildSampleImages -Language node -BaseImagePath 4-node16-core-tools  -ExtensionBundlePath "C:\Functions\azure-functions-extension-bundles\artifacts\Microsoft.Azure.Functions.ExtensionBundle.Preview.4.5.0_any-any.zip" -Acr <acr>.azurecr.io -DockerPush True
    #>
    Param(
        [Parameter(Mandatory = $true, HelpMessage = "The language to build the base image, values include node,dotnet-isolated,dotnet,powershell,java")] [string]$Language,
        [Parameter(Mandatory = $true, HelpMessage = "The core-tools image version in https://github.com/Azure/azure-functions-docker. Example values 4-powershell7.2-core-tools,4-node16-core-tools")] [string]$BaseImagePath,
        [Parameter(Mandatory = $false, HelpMessage = "If building bundles locally and using them for testing, use this parameter. Build instructions on https://github.com/Azure/azure-functions-extension-bundles. e.g.The path is the path azure-functions-extension-bundles\artifacts\Microsoft.Azure.Functions.ExtensionBundle.Preview.4.5.0_any-any.zip")][AllowNull()][string]$ExtensionBundlePath = $null,
        [Parameter(Mandatory = $false, HelpMessage = "The azure container registry to push to myacr.azure.io")][AllowNull()][string]$Acr = $null,
        [Parameter(Mandatory = $false, HelpMessage = "If the tagged image needs to be pushed to the docker hub")][bool]$DockerPush = $false
    ) #end param
    Write-Host "------------------------------------------------------------------------------------------------------------------------------"
    Write-Host "Using base image mcr.microsoft.com/azure-functions/${Language}:${BaseImagePath}" -ForegroundColor Green
    # build the DLL so that we can use this when we do the rebuild
    Write-Host "------------------------------------------------------------------------------------------------------------------------------"
    Write-Host "Cleaning and building Project" -ForegroundColor Green
    dotnet clean
    dotnet build /p:Configuration=Release
    $BuildDate = (Get-Date).ToString("yyyyMMdd")
    $TargetFileLocation="./samples/samples-$Language"
    # The docker file goes to resources folder
    $TargetDockerFile="${TargetFileLocation}\Docker.$Language"
    Write-Host "------------------------------------------------------------------------------------------------------------------------------"
    #If a locally built extension bundle is specified , add it
    #If an override on the ExtensionBundlePath is specified then use this in the image on the docker file.    
    if (!$ExtensionBundlePath) {
        Write-Host "Creating docker file to build with tag 'func-az-kusto-${Language}:$BuildDate'" -ForegroundColor Green
        ((Get-Content -path .\samples\docker\Docker-template.dockerfile -Raw) -creplace 'imagename', "mcr.microsoft.com/azure-functions/node:4-node16-core-tools" -creplace 'bundlepath', $ExtensionBundlePath) | Set-Content -Path $TargetDockerFile
    }
    else {
        $EscapedPath = (Get-Item $ExtensionBundlePath | Resolve-Path -Relative) -replace "\\", "/"
        Copy-Item -Path $EscapedPath $TargetFileLocation/Microsoft.Azure.Functions.ExtensionBundle.zip
        Write-Host "Creating docker file to build with tag 'func-az-kusto-${Language}:$BuildDate' with extension bundle copy on path $EscapedPath" -ForegroundColor Green -BackgroundColor White
        ((Get-Content -path .\samples\docker\Docker-template.dockerfile -Raw) -creplace 'imagename', "mcr.microsoft.com/azure-functions/node:4-node16-core-tools" -creplace 'bundlepath', $EscapedPath -replace '#COPY', "COPY ${TargetFileLocation}/Microsoft.Azure.Functions.ExtensionBundle.zip ") | Set-Content -Path $TargetDockerFile
    }
    if(!$Acr)
    {
        $TagCreated = "${Language}:${BuildDate}"
        Write-Host "Creating docker tag $TagCreated" -ForegroundColor Green
        docker build -t func-az-kusto-$TagCreated -t func-az-kusto-${Language}:latest -f $TargetDockerFile .
    }
    else {
        $TagCreated = "$Acr/func-az-kusto-${Language}:$BuildDate"
        Write-Host "Creating docker tag $TagCreated" -ForegroundColor Green
        docker build -t $TagCreated -t $Acr/func-az-kusto-${Language}:latest -f $TargetDockerFile .
        if($true -eq $DockerPush)
        {
            docker push $TagCreated
        }
    }
    
    if ($?) {
        Write-Host "Image build for $Language complete 'func-az-kusto-${Language}:$BuildDate'" -ForegroundColor Green
    }
    else {
        Write-Host "Image build for $Language complete 'func-az-kusto-${Language}:$BuildDate' failed. Is docker running ?" -ForegroundColor Red
    }
    Remove-Item  $TargetFileLocation/Microsoft.Azure.Functions.ExtensionBundle.zip -ErrorAction Ignore
}