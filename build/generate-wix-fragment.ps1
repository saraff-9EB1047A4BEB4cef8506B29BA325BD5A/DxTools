param(
    [Parameter(Mandatory = $true)]
    [string]$PublishDir,
    [Parameter(Mandatory = $true)]
    [string]$OutputFile
)

$ErrorActionPreference = "Stop"

$publishRoot = (Resolve-Path -LiteralPath $PublishDir).Path.TrimEnd('\')
$outputFull = [System.IO.Path]::GetFullPath($OutputFile)
$outputDirectory = [System.IO.Path]::GetDirectoryName($outputFull)
if (-not [string]::IsNullOrWhiteSpace($outputDirectory)) {
    New-Item -ItemType Directory -Force -Path $outputDirectory | Out-Null
}

$files = Get-ChildItem -LiteralPath $publishRoot -Recurse -File |
    Where-Object { $_.Extension -notin @('.pdb', '.xml') }

if (-not $files) {
    throw "Publish directory '$publishRoot' contains no files."
}

function New-StableId {
    param([string]$Prefix, [string]$Value)
    $md5 = [System.Security.Cryptography.MD5]::Create()
    try {
        $hash = $md5.ComputeHash([System.Text.Encoding]::UTF8.GetBytes($Value.ToLowerInvariant()))
    }
    finally {
        $md5.Dispose()
    }

    $hex = -join ($hash[0..6] | ForEach-Object { $_.ToString("x2") })
    return "$Prefix$hex"
}

function New-StableGuid {
    param([string]$Value)
    $md5 = [System.Security.Cryptography.MD5]::Create()
    try {
        $hash = $md5.ComputeHash([System.Text.Encoding]::UTF8.GetBytes("DxTools|$($Value.ToLowerInvariant())"))
    }
    finally {
        $md5.Dispose()
    }

    $bytes = New-Object byte[] 16
    [Array]::Copy($hash, $bytes, 16)
    return ([guid]::new($bytes)).ToString("B").ToUpperInvariant()
}

$root = @{ Dirs = [ordered]@{}; Files = @() }

foreach ($file in $files) {
    $relative = $file.FullName.Substring($publishRoot.Length).TrimStart('\')
    $parts = $relative -split '\\'
    $directoryParts = @()
    if ($parts.Count -gt 1) {
        $directoryParts = $parts[0..($parts.Count - 2)]
    }

    $node = $root
    foreach ($part in $directoryParts) {
        if (-not $node.Dirs.Contains($part)) {
            $node.Dirs[$part] = @{ Dirs = [ordered]@{}; Files = @() }
        }

        $node = $node.Dirs[$part]
    }

    $node.Files += [pscustomobject]@{
        Name = $parts[-1]
        FullName = $file.FullName
        Relative = $relative
    }
}

$settings = New-Object System.Xml.XmlWriterSettings
$settings.Indent = $true
$settings.IndentChars = "  "
$settings.Encoding = New-Object System.Text.UTF8Encoding($false)
$settings.NewLineChars = "`r`n"

$namespace = "http://wixtoolset.org/schemas/v4/wxs"
$writer = [System.Xml.XmlWriter]::Create($outputFull, $settings)
$componentRefs = New-Object System.Collections.Generic.List[string]

function Write-Component {
    param($file)

    $componentId = New-StableId -Prefix "cmp_" -Value $file.Relative
    $fileId = New-StableId -Prefix "fil_" -Value $file.Relative
    $guid = New-StableGuid -Value $file.Relative

    $writer.WriteStartElement("Component", $namespace)
    $writer.WriteAttributeString("Id", $componentId)
    $writer.WriteAttributeString("Guid", $guid)

    $writer.WriteStartElement("File", $namespace)
    $writer.WriteAttributeString("Id", $fileId)
    $writer.WriteAttributeString("Name", $file.Name)
    $writer.WriteAttributeString("Source", $file.FullName)
    $writer.WriteAttributeString("KeyPath", "yes")
    $writer.WriteEndElement()

    $writer.WriteEndElement()
    $componentRefs.Add($componentId)
}

function Write-DirectoryNode {
    param($node, [string]$relativePath)

    foreach ($file in ($node.Files | Sort-Object Relative)) {
        Write-Component -file $file
    }

    foreach ($name in $node.Dirs.Keys) {
        $childPath = if ($relativePath) { "$relativePath\$name" } else { $name }
        $directoryId = New-StableId -Prefix "dir_" -Value $childPath

        $writer.WriteStartElement("Directory", $namespace)
        $writer.WriteAttributeString("Id", $directoryId)
        $writer.WriteAttributeString("Name", $name)

        Write-DirectoryNode -node $node.Dirs[$name] -relativePath $childPath

        $writer.WriteEndElement()
    }
}

try {
    $writer.WriteStartDocument()
    $writer.WriteStartElement("Wix", $namespace)
    $writer.WriteStartElement("Fragment", $namespace)
    $writer.WriteStartElement("DirectoryRef", $namespace)
    $writer.WriteAttributeString("Id", "INSTALLFOLDER")

    Write-DirectoryNode -node $root -relativePath ""

    $writer.WriteEndElement()

    $writer.WriteStartElement("ComponentGroup", $namespace)
    $writer.WriteAttributeString("Id", "AppFiles")
    foreach ($componentId in $componentRefs) {
        $writer.WriteStartElement("ComponentRef", $namespace)
        $writer.WriteAttributeString("Id", $componentId)
        $writer.WriteEndElement()
    }

    $writer.WriteEndElement()
    $writer.WriteEndElement()
    $writer.WriteEndElement()
    $writer.WriteEndDocument()
}
finally {
    $writer.Dispose()
}

Write-Output "Generated WiX fragment: $outputFull ($($componentRefs.Count) components)"
