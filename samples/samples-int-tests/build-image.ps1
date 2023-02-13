Function ReBuildImage
{
    Param(
        [Parameter(Mandatory=$true)]
        [string]$Language,
        [Parameter(Mandatory=$true)]
        [string]$BaseImagePath,
        [string]$ExtensionBundlePath
    ) #end param
    Write-Host "Using base image mcr.microsoft.com/azure-functions/${Language}:${BaseImagePath}" 

    #If an override on the ExtensionBundlePath is specified then use this in the image on the docker file.
    if ($null -eq $ExtensionBundlePath) {
        $ExtensionBundlePath = "Using extension bundle from $ExtensionBundlePath" 
    }
    # build the DLL so that we can use this when we do the rebuild
    dotnet clean
    dotnet build /p:Configuration=Release
    ((Get-Content -path .\Docker-template.dockerfile -Raw) -replace '$$imagename$$',"mcr.microsoft.com/azure-functions/${Language}:${BaseImagePath}" -replace '$$bundlepath$$',$ExtensionBundlePath) | Set-Content -Path "Docker.$Language"

} #end function ReBuildImage