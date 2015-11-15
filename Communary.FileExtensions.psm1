# Communary.FileExtensions
# 2015 Øyvind Kallstad (@okallstad)

Add-Type  -Path $PSScriptRoot\FileExtensions.cs
function Invoke-FastFind {
    <#
        .SYNOPSIS
            Search for files and folders.
        .DESCRIPTION
            This function uses WIN32 API to perform faster searching for files and folders. It also supports large paths.
        .EXAMPLE
            Invoke-FastFind
            Will list all files and folders in the current directory.
        .EXAMPLE
            Invoke-FastFind c:\
            Will list all files and folders in c:\
        .EXAMPLE
            Invoke-FastFind c:\ prog*
            Will list all files and folders in c:\ that starts with 'prog'.
        .EXAMPLE
            Invoke-FastFind c:\ -Directory
            Will list all directories in c:\
        .EXAMPLE
            Invoke-FastFind c:\ -Directory -Hidden
            Will list all hidden directories in c:\
        .EXAMPLE
            Invoke-FastFind c:\ -System -Hidden -AttributeFilterMode Exclude
            Will list all files and folders in c:\ that don't have the System and Hidden attributes set.
        .EXAMPLE
            Invoke-FastFind c:\ -Hidden -System -Archive -AttributeFilterMode Strict
            Will list all files and folders in c:\ that only have the Hidden, System and Archive attributes set.
        .NOTES
            Author: Øyvind Kallstad
            Date: 13.11.2015
            Version: 1.0
        .LINK
            https://communary.wordpress.com/
            https://github.com/gravejester/Communary.FileExtensions
    #>
    [CmdletBinding()]
    param (
        # Path where search starts from. The default value is the current directory.
        [Parameter(Position = 1)]
        [ValidateNotNullOrEmpty()]
        [string[]] $Path = ((Get-Location).Path),

        # Search filter. Accepts wildcards; * and ?. The default value is '*'.
        [Parameter(Position = 2)]
        [string] $Filter = '*',

        [Parameter()]
        [Alias('f')]
        [switch] $File,

        [Parameter()]
        [Alias('d')]
        [switch] $Directory,

        [Parameter()]
        [switch] $Hidden,

        [Parameter()]
        [switch] $System,

        [Parameter()]
        [switch] $ReadOnly,

        [Parameter()]
        [switch] $Compressed,

        [Parameter()]
        [switch] $Archive,

        [Parameter()]
        [switch] $ReparsePoint,
        
        # Choose the filter mode for attribute filtering. Valid choices are 'Include', 'Exclude' and 'Strict'. Default is 'Include'.
        [Parameter()]
        [ValidateSet('Include','Exclude','Strict')]
        [string] $AttributeFilterMode = 'Include',

        # Perform recursive search.
        [Parameter()]
        [Alias('r')]
        [switch] $Recurse,

        # Depth of recursive search. Default is null (unlimited recursion).
        [Parameter()]
        [nullable[int]] $Depth = $null,

        # Use a larger buffer for the search, which *can* increase performance. Not supported for operating systems older than Windows Server 2008 R2 and Windows 7.
        [Parameter()]
        [switch] $LargeFetch
    )

    if ($PSBoundParameters['File'] -and $PSBoundParameters['Directory']) {
        $File = $false
        $Directory = $false
    }

    foreach ($thisPath in $Path) {
        #if (Test-Path -Path $thisPath) {
            
            # adds support for relative paths
            #$resolvedPath = (Resolve-Path -Path $thisPath).Path
            #$resolvedPath = $resolvedPath.Replace('Microsoft.PowerShell.Core\FileSystem::','')
            $resolvedPath = $thisPath

            # handle a quirk where \ at the end of a non-UNC, non-root path failes
            if (-not ($resolvedPath.ToString().StartsWith('\\'))) {
                if ($resolvedPath.ToString().EndsWith('\')) {
                    if (-not($resolvedPath -eq ([System.IO.Path]::GetPathRoot($resolvedPath)))) {
                        $resolvedPath = $resolvedPath.ToString().TrimEnd('\')
                    }
                }
            }

            # call FastFind to perform search
            [Communary.FileExtensions]::FastFind($resolvedPath, $Filter, $File, $Directory, $Recurse, $Depth, $true, $true, $LargeFetch, $Hidden, $System, $ReadOnly, $Compressed, $Archive, $ReparsePoint, $AttributeFilterMode)
        #}
        #else {
        #    Write-Warning "$thisPath - Invalid path"
        #}
    }    
}

function Get-DirectoryInfo {
    <#
        .SYNOPSIS
            Get general information about specified directory.
        .DESCRIPTION
            Get information about number of files and folders and total size for a given directory.
            This function supports long paths, both local and UNC.
            Note that the SizeOnDisk value is only an approximation!
        .EXAMPLE
            Get-DirectoryInfo
            Get directory information about current directory.
        .EXAMPLE
            Get-DirectoryInfo c:\windows
            Get directory information about the c:\windows directory
        .NOTES
            Author: Øyvind Kallstad
            Date: 13.11.2015
            Version: 1.0
        .LINK
            https://communary.wordpress.com/
            https://github.com/gravejester/Communary.FileExtensions
    #>
    [CmdletBinding()]
    param (
        [Parameter(Position = 1)]
        [ValidateNotNullOrEmpty()]
        [string[]] $Path = ((Get-Location).Path)
    )

    foreach ($thisPath in $Path) {

        $sectorSize = [Communary.FileExtensions]::GetSectorSize($thisPath)
        $pathContents = [Communary.FileExtensions]::FastFind($thisPath, '*', $false, $false, $true, $null, $true, $true, $false, $false, $false, $false, $false, $false, $false, 'Include')
        $numberOfFolders = ($pathContents | Where-Object {$_.Attributes.HasFlag([System.IO.FileAttributes]::Directory)}).Count
        $numberOfFiles = $pathContents.count - $numberOfFolders
        [long]$totalSize = 0
        foreach ($item in $pathContents) {
            $totalSize = $totalSize + $item.FileSize
        }
        [long]$sizeOnDisk = (($totalSize + $sectorSize - 1) / $sectorSize) * $sectorSize
    
        Write-Output (,([PSCustomObject] @{
            Path = $thisPath
            NumberOfDirectories = $numberOfFolders
            NumberOfFiles = $numberOfFiles
            TotalSize = $totalSize
            SizeOnDisk = $sizeOnDisk
        }))
    }
}

function Remove-File {
    <#
        .SYNOPSIS
            Delete file(s).
        .DESCRIPTION
            Delete one or more files. This function supports long paths, both local and UNC.
        .EXAMPLE
            Remove-File c:\temp\tempfile.txt
            Will remove the file c:\temp\tempfile.txt after prompting you for confirmation.
        .EXAMPLE
            c:\temp\tempfile.txt | Remove-File
            Will remove the file c:\temp\tempfile.txt after prompting you for confirmation.
        .EXAMPLE
            Remove-File c:\temp\tempfile.txt -WhatIf
            Will give you information about what the function would perform if used without the -WhatIf parameter.
        .EXAMPLE
            Remove-File c:\temp\tempfile.txt -Force
            Will remove the file c:\temp\tempfile.txt without prompting you for confirmation.
        .NOTES
            Author: Øyvind Kallstad
            Date: 13.11.2015
            Version: 1.0
        .LINK
            https://communary.wordpress.com/
            https://github.com/gravejester/Communary.FileExtensions
    #>
    [CmdletBinding(SupportsShouldProcess=$true, ConfirmImpact='High')]
    param (
        [Parameter(Position = 0, ValueFromPipeline = $true, ValueFromPipelineByPropertyName = $true)]
        [ValidateNotNullOrEmpty()]
        [string[]] $Path,

        [Parameter()]
        [switch] $Force
    )

    PROCESS {
        foreach ($thisPath in $Path) {
            if ($Force -or $PSCmdlet.ShouldProcess($thisPath,'Delete')) {
                try {
                    [Communary.FileExtensions]::DeleteFile($thisPath)
                }
                    
                catch {
                    Write-Warning "Failed to remove $($thisPath): $($_.Exception.Message)"
                }
            }
        }
    }
}

function Remove-Directory {
    <#
        .SYNOPSIS
            Delete folder(s).
        .DESCRIPTION
            Delete one or more directory. This function supports long paths, both local and UNC.
            Function will fail if directory is not empty.
        .EXAMPLE
            Remove-Directory c:\temp\tempfolder
            Will remove the directory c:\temp\tempfolder after prompting you for confirmation.
        .EXAMPLE
            c:\temp\tempfolder | Remove-Directory
            Will remove the directory c:\temp\tempfolder after prompting you for confirmation.
        .EXAMPLE
            Remove-Directory c:\temp\tempfolder -WhatIf
            Will give you information about what the function would perform if used without the -WhatIf parameter.
        .EXAMPLE
            Remove-Directory c:\temp\tempfolder -Force
            Will remove the directory c:\temp\tempfolder without prompting you for confirmation.
        .NOTES
            Author: Øyvind Kallstad
            Date: 14.11.2015
            Version: 1.0
        .LINK
            https://communary.wordpress.com/
            https://github.com/gravejester/Communary.FileExtensions
    #>
    [CmdletBinding(SupportsShouldProcess=$true, ConfirmImpact='High')]
    param (
        [Parameter(Position = 0, ValueFromPipeline = $true, ValueFromPipelineByPropertyName = $true)]
        [ValidateNotNullOrEmpty()]
        [string[]] $Path,

        [Parameter()]
        [switch] $Force
    )

    PROCESS {
        foreach ($thisPath in $Path) {
            if ($Force -or $PSCmdlet.ShouldProcess($thisPath,'Delete')) {
                try {
                    [Communary.FileExtensions]::DeleteDirectory($thisPath)
                }
                    
                catch {
                    Write-Warning "Failed to remove $($thisPath): $($_.Exception.Message)"
                }
            }
        }
    }
}

function New-Tempfile {
    <#
        .SYNOPSIS
            Create a new temporary file.
        .DESCRIPTION
            This function creates a uniquely named, zero-byte temporary file with a .TMP file extension.
            The temporary file is created within the user’s temporary folder and the FileInfo object of that file is returned.
        .NOTES
            Author: Øyvind Kallstad
            Date: 13.11.2015
            Version: 1.0
        .LINK
            https://communary.wordpress.com/
            https://github.com/gravejester/Communary.FileExtensions
    #>
    try {
        $tmpFile = [System.IO.Path]::GetTempFileName()
        $tmpFileObject = Get-Item -Path $tmpFile
        Write-Output $tmpFileObject
    }
    catch {
        Write-Warning $_.Exception.Message
    }
}

function Get-TempFolder {
    <#
        .SYNOPSIS
            Returns the current user's temporary folder.
        .DESCRIPTION
            Returns the current user's temporary folder.
        .NOTES
            Author: Øyvind Kallstad
            Date: 13.11.2015
            Version: 1.0
        .LINK
            https://communary.wordpress.com/
            https://github.com/gravejester/Communary.FileExtensions
    #>
    $tmpFolder = [System.IO.Path]::GetTempPath()
    $tmpFolderObject = Get-Item -Path $tmpFolder
    Write-Output $tmpFolderObject
}

function Get-RandomFileName {
    <#
        .SYNOPSIS
            Returns a random folder name or file name.
        .DESCRIPTION
            Returns a cryptographically strong, random string that can be used as either a folder name or a file name.
        .NOTES
            Author: Øyvind Kallstad
            Date: 13.11.2015
            Version: 1.0
        .LINK
            https://communary.wordpress.com/
            https://github.com/gravejester/Communary.FileExtensions
    #>
    [System.IO.Path]::GetRandomFileName()
}

function Resolve-PathEx {
    <#
        .SYNOPSIS
            Resolve-Path extended to also work with files that don't exist.
        .DESCRIPTION
            You can use Resolve-PathEx when you want to handle both filenames and paths in a single parameter in your functions.
            The function returns an object, and includes the resolved path as well as a boolean indicating whether the file
            exists or not. Wildcards are supported for both path and filename.
        .EXAMPLE
            Resolve-Path *.ps1
            Will resolve full path of all files in the current directory with the ps1 file extension.
        .EXAMPLE
            Resolve-PathEx c:\program*\windows*\w*.exe
            Will resolve full path of all exe files beginning with w in any folders of the root of C: that starts with 'progra',
            and all subfolders of these that start with 'windows'.
        .EXAMPLE
            Resolve-Path
            Will resolve the current path.
        .EXAMPLE
            Resolve-Path nosuchfile.txt
            Will resolve the full path of the file, even though it doesn't exist.
        .LINK
            https://communary.wordpress.com/
            https://github.com/gravejester/Communary.FileExtensions
        .NOTES
            Author: Øyvind Kallstad
            Date: 13.11.2014
            Version: 1.0
    #>
    [CmdletBinding()]
    param (
        [Parameter(Position = 0, ValueFromPipeline, ValueFromPipelinebyPropertyName)]
        [string[]] $Path = '.\'
    )
 
    PROCESS{
        foreach ($thisPath in $Path) {
            try {
                # first try to resolve using the whole path
                [array]$resolvedPath += (Resolve-Path -Path $thisPath -ErrorAction SilentlyContinue | Select-Object -ExpandProperty Path)
 
                # if that didn't work, split to get the path only
                if ([string]::IsNullOrEmpty($resolvedPath)) {
                    $pathOnly = Split-Path $thisPath
                    # if no path returned, add current directory as path
                    if ([string]::IsNullOrEmpty($pathOnly)) {
                        $pathOnly = '.\'
                    }
                    # try to resolve again using only the path
                    $pathOnlyResolve = (Resolve-Path -Path $pathOnly -ErrorAction SilentlyContinue | Select-Object -ExpandProperty Path)
                    # if successfull
                    if (-not([string]::IsNullOrEmpty($pathOnlyResolve))) {
                        # add the path and the filename back together
                        foreach ($p in $pathOnlyResolve) {
                            $pathAndFile = Join-Path -Path $p -ChildPath (Split-Path -Path $thisPath -Leaf)
                            $exists = Test-Path $pathAndFile
                            Write-Output (,([PSCustomObject] [Ordered] @{
                                Path = $pathAndFile
                                Exists = $exists
                            }))
                        }
                    }
                    # if we still are unable to resolve, the path most likely don't exist
                    else {
                        Write-Warning "Unable to resolve $pathOnly"
                    }
                }
                else {
                    foreach ($item in $resolvedPath) {
                        $exists = Test-Path $item
                        Write-Output (,([PSCustomObject] [Ordered] @{
                            Path = $item
                            Exists = $exists
                        }))
                    }   
                }
            }
 
            catch {
                Write-Warning $_.Exception.Message
            }
        }
    }
}

function Invoke-Touch {
    <#
        .SYNOPSIS
            PowerShell inplementation of the Unix/Linux utility called touch.
        .DESCRIPTION
            Touch let's you update the access date and / or modification date of a file. If the file don't exist, an empty file will be created
            unless you use the DoNotCreateNewFile parameter. This implementation have the original parameter names added as
            aliases, so if you are familiar with the original touch utility it should be easy to use this one.
        .EXAMPLE
            Invoke-Touch newfile
            Will create a new empty file called 'newfile' in the current folder.
        .EXAMPLE
            Invoke-Touch newfile3 -DateTime '10.10.2014'
            Will create a new empty file called 'newfile3' with the provided date.
        .EXAMPLE
            Invoke-Touch newfile -r newfile3
            Will update the timestamp of 'newfile' using 'newfile3' as a reference.
        .LINK
            https://gist.github.com/gravejester/f4934a5ce16c652d11d3
        .NOTES
            Author: Øyvind Kallstad
            Date: 13.11.2014
            Version: 1.0
    #>
    [CmdletBinding(ConfirmImpact = 'Low',SupportsShouldProcess, DefaultParameterSetName = 'UserDateTime')]
    param (
        # Filename and/or path.
        [Parameter(Position = 0, ValueFromPipeline, ValueFromPipelinebyPropertyName)]
        [string[]] $Path,
 
        # File to use as a timestamp reference.
        [Parameter(ParameterSetName = 'ReferenceDateTime')]
        [Alias('r')]
        [string] $Reference,
 
        # Timestamp offset in seconds.
        [Parameter()]
        [Alias('B','F')]
        [int] $OffsetSeconds = 0,
 
        # Used to override the timestamp. If omitted the current date and time will be used.
        [Parameter(ParameterSetName = 'UserDateTime')]
        [Alias('t','d')]
        [string] $DateTime,
 
        # Update Last Access Time.
        [Parameter()]
        [Alias('a')]
        [switch] $AccessTime,
 
        # Update Last Write Time.
        [Parameter()]
        [Alias('m','w')]
        [switch] $WriteTime,
 
        # Switch to override the basic functionality of creating a new file if it don't exist already.
        [Parameter()]
        [Alias('c')]
        [switch] $DoNotCreateNewFile,
 
        [Parameter()]
        [switch] $PassThru
    )
 
    BEGIN {
        
        try {
            # use timestamp from a reference file
            if (-not([string]::IsNullOrEmpty($Reference))) {
                if (Test-Path $Reference) {
                    $referenceFile = Get-ChildItem -Path $Reference
                    $newLastAccessTime = ($referenceFile.LastAccessTime).AddSeconds($OffsetSeconds)
                    $newLastWriteTime = ($referenceFile.LastWriteTime).AddSeconds($OffsetSeconds)
                    Write-Verbose "Using timestamp from $Reference"
                }
                else {
                    Write-Warning "$Reference not found!"
                }
            }
 
            # use timestamp from user input
            elseif (-not([string]::IsNullOrEmpty($DateTime))) {
                $userDateTime = [DateTime]::Parse($DateTime,[CultureInfo]::CurrentCulture,[System.Globalization.DateTimeStyles]::NoCurrentDateDefault)
                Write-Verbose "Using timestamp from user input: $DateTime (Parsed: $($userDateTime))"
            }
 
            # use timestamp from current date/time
            else {
                $currentDateTime = (Get-Date).AddSeconds($OffsetSeconds)
                $newLastAccessTime = $currentDateTime
                $newLastWriteTime = $currentDateTime
                Write-Verbose "Using timestamp from current date/time: $currentDateTime"
            }
        }
        catch {
            Write-Warning $_.Exception.Message
        }
    }
 
    PROCESS {
        foreach ($thisPath in $Path) {
            
            try {
                $thisPathResolved = Resolve-PathEx -Path $thisPath
 
                foreach ($p in $thisPathResolved.Path) {
                    Write-Verbose "Resolved path: $p"
 
                    # if file is not found, and it's ok to create a new file, create it!
                    if (-not(Test-Path $p)) {
                        if ($DoNotCreateNewFile) {
                            Write-Verbose "$p not created"
                            return
                        }
                        else {
                            if ($PSCmdlet.ShouldProcess($p, 'Create File')) {
                                $null = New-Item -path $p -ItemType 'File' -ErrorAction 'Stop'
                            }
                        }
                    }
 
                    # get fileinfo object
                    $fileObject = Get-ChildItem $p -ErrorAction SilentlyContinue

                    if (-not([string]::IsNullOrEmpty($fileObject))) {
                        # handle date & time if datetime parameter is used
                        if (-not([string]::IsNullOrEmpty($DateTime))) {
 
                            # if parsed datetime object contains time
                            if ([bool]$userDateTime.TimeOfDay.Ticks) {
                                Write-Verbose 'Found time in datetime'
                                $userTime = $userDateTime.ToLongTimeString()
                            }
                            # else, get time from file
                            else {
                                Write-Verbose 'Did not find time in datetime - using time from file'
                                $userTime = $fileObject.LastAccessTime.ToLongTimeString()
                            }
 
                            # if parsed datetime object contains date
                            if ([bool]$userDateTime.Date.Ticks) {
                                Write-Verbose 'Found date in datetime'
                                $userDate = $userDateTime.ToShortDateString()
                            }

                            # else, get date from file
                            else {
                                Write-Verbose 'Did not find date in datetime - using date from file'
                                $userDate = $fileObject.LastAccessTime.ToShortDateString()
                            }
 
                            # parse the new datetime
                            $parsedNewDateTime = [datetime]::Parse("$userDate $userTime")
 
                            # add offset and save to the appropriate variables
                            $newLastAccessTime = $parsedNewDateTime.AddSeconds($OffsetSeconds)
                            $newLastWriteTime = $parsedNewDateTime.AddSeconds($OffsetSeconds)
                        }
                    }
 
                    if ($PSCmdlet.ShouldProcess($p, 'Update Timestamp')) {
                        # if neither -AccessTime nor -WriteTime is used, update both Last Access Time and Last Write Time
                        if ((-not($AccessTime)) -and (-not($WriteTime))) {
                            $fileObject.LastAccessTime = $newLastAccessTime
                            $fileObject.LastWriteTime = $newLastWriteTime
                        }
 
                        else {
                            if ($AccessTime) { $fileObject.LastAccessTime = $newLastAccessTime }
                            if ($WriteTime) { $fileObject.LastWriteTime = $newLastWriteTime }
                        }
                    }
                }
 
                if ($PassThru) {
                    Write-Output (Get-ChildItem $p)
                }
            }
 
            catch {
                Write-Warning $_.Exception.Message
            }
        }
    }
}

function Set-FileAttributes {
    <#
        .SYNOPSIS
            Set file attributes.
        .DESCRIPTION
            This function lets you set file attributes for file(s) or folder(s). Long paths are supported.
        .NOTES
            Author: Øyvind Kallstad
            Date: 13.11.2015
            Version: 1.0
        .LINK
            https://communary.wordpress.com/
            https://github.com/gravejester/Communary.FileExtensions
    #>
    [CmdletBinding(DefaultParameterSetName = 'default')]
    param (
        [Parameter(Position = 1, ValueFromPipeline = $true, ValueFromPipelinebyPropertyname = $true)]
        [ValidateNotNullOrEmpty()]
        [string[]] $Path,

        [Parameter(ParameterSetName = 'default')]
        [switch] $Archive,

        [Parameter(ParameterSetName = 'default')]
        [switch] $Hidden,

        [Parameter(ParameterSetName = 'normal')]
        [switch] $Normal,

        [Parameter(ParameterSetName = 'default')]
        [switch] $ReadOnly,

        [Parameter(ParameterSetName = 'default')]
        [switch] $System
    )

    PROCESS {
        foreach ($thisPath in $Path) {   
            if ($Normal) {
                $attributes = [System.IO.FileAttributes]::Normal
            }
                
            else {
                $attributesToAdd = @()
                if ($Archive) {$attributesToAdd += [System.IO.FileAttributes]::Archive}
                if ($Hidden) {$attributesToAdd += [System.IO.FileAttributes]::Hidden}
                if ($ReadOnly) {$attributesToAdd += [System.IO.FileAttributes]::ReadOnly}
                if ($System) {$attributesToAdd += [System.IO.FileAttributes]::System}
                foreach ($thisAttribute in $attributesToAdd) {
                    $attributes = $attributes -bor $thisAttribute
                }
            }
                
            try {
                [Communary.FileExtensions]::AddFileAttributes($thisPath, $attributes)
            }
                
            catch {
                Write-Warning "Failed to set attributes for $($thisPath): $($_.Exception.Message)"
            }
        }
    }
}

function Get-FileAttributes {
    <#
        .SYNOPSIS
            Get file attributes.
        .DESCRIPTION
            This function lets you get file attributes for file(s) or folder(s). Long paths are supported.
        .NOTES
            Author: Øyvind Kallstad
            Date: 13.11.2015
            Version: 1.0
        .LINK
            https://communary.wordpress.com/
            https://github.com/gravejester/Communary.FileExtensions
    #>
    [CmdletBinding()]
    param (
        [Parameter(Position = 1, ValueFromPipeline = $true, ValueFromPipelinebyPropertyname = $true)]
        [ValidateNotNullOrEmpty()]
        [string[]] $Path
    )

    PROCESS {
        foreach ($thisPath in $Path) {
            try {
                [System.IO.FileAttributes]$attributes = [Communary.FileExtensions]::GetFileAttributes($thisPath)
                Write-Output $attributes
            }
            catch {
                Write-Warning "Failed to get file attributes for $($thisPath): $($_.Exception.Message)"
            }
        }
    }
}

function ConvertTo-UNCPath {
    <#
        .SYNOPSIS
            Convert a path to UNC path
        .DESCRIPTION
            Convert a path to UNC path
        .NOTES
            Author: Øyvind Kallstad
            Date: 26.10.2015
            Version: 1.0
        .INPUTS
            System.String
        .OUTPUTS
            System.String
        .LINK
            https://communary.wordpress.com/
            https://github.com/gravejester/Communary.FileExtensions
    #>
    [CmdletBinding()]
    param (
        [Parameter(Position = 2, ValueFromPipeline, ValueFromPipelineByPropertyName)]
        [ValidateNotNullOrEmpty()]
        [string] $Path,

        [Parameter(Position = 1)]
        [ValidateNotNullOrEmpty()]
        [string] $ComputerName
    )

    $pathRoot = [System.IO.Path]::GetPathRoot($Path)
    Write-Output ("\\$($ComputerName)$(($Path).Replace($pathRoot, "\$($Path[0])$\"))")
}

function Get-FileOwner {
    [CmdletBinding()]
    param (
        [Parameter(Position = 1, ValueFromPipeline = $true, ValueFromPipelinebyPropertyname = $true)]
        [ValidateNotNullOrEmpty()]
        [string[]] $Path
    )

    PROCESS {
        foreach ($thisPath in $Path) {
            try {
                $fileOwner = [Communary.FileExtensions]::GetFileOwner($thisPath)
                Write-Output $fileOwner
            }
            catch {
                Write-Warning "Failed to get file owner for $($thisPath): $($_.Exception.Message)"
            }
        }
    }
}

function Test-Exist {
    [CmdletBinding()]
    param (
        [Parameter(Position = 1, ValueFromPipeline = $true, ValueFromPipelinebyPropertyname = $true)]
        [string[]] $Path
    )

    PROCESS {
        foreach ($thisPath in $Path) {
            if ($thisPath) {
                $pathSplit = $thisPath.Split('\', [System.StringSplitOptions]::RemoveEmptyEntries)
                $filter = $pathSplit[-1]
                $searchPath = $pathSplit[0..($pathSplit.Count - 2)] -join '\'
                if ([bool](Invoke-FastFind -Path $searchPath -Filter $filter)) {
                    Write-Output $true
                }
                else {
                    Write-Output $false
                }
            }
            else {
                Write-Output $false
            }
        }
    }
}

function Get-LastLines {
    [CmdletBinding()]
    param (
        [Parameter(Position = 1, ValueFromPipeline = $true, ValueFromPipelinebyPropertyname = $true)]
        [ValidateNotNullOrEmpty()]
        [string] $Path,

        [Parameter(Position = 2)]
        [ValidateRange(1,[int]::MaxValue)]
        [int] $Lines = 1
    )

    Write-Output ([Communary.FileExtensions]::ReadLastLines($Path, $Lines))
}


Set-Alias -Name 'touch' -Value 'Invoke-Touch' -Force
Set-Alias -Name 'ff' -Value 'Invoke-FastFind' -Force
Set-Alias -Name 'du' -Value 'Get-DirectoryInfo' -Force